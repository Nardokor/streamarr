import React, { useCallback, useEffect, useRef, useState } from 'react';
import ReactDOM from 'react-dom';
import IconButton from 'Components/Link/IconButton';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import { icons } from 'Helpers/Props';
import Channel from 'typings/Channel';
import formatBytes from 'Utilities/Number/formatBytes';
import {
  UnmatchedFile,
  unmatchedFileReasonLabel,
  useAssignUnmatchedFile,
  useDismissUnmatchedFile,
  useUnmatchedFilesByCreator,
} from './Import/useUnmatchedFiles';
import { formatDate } from './creatorUtils';
import styles from './CreatorUnmatchedSection.css';

interface UnmatchedFileRowProps {
  file: UnmatchedFile;
  channels: Channel[];
}

function UnmatchedFileRow({ file, channels }: UnmatchedFileRowProps) {
  const { dismiss, isDismissing } = useDismissUnmatchedFile(file.id, file.creatorId);
  const { assign, isAssigning } = useAssignUnmatchedFile(file.id, file.creatorId);
  const [pickerOpen, setPickerOpen] = useState(false);
  const [pickerPos, setPickerPos] = useState({ top: 0, left: 0 });
  const [playerOpen, setPlayerOpen] = useState(false);
  const buttonRef = useRef<HTMLDivElement>(null);

  const handleOpenPicker = useCallback(() => {
    if (buttonRef.current) {
      const rect = buttonRef.current.getBoundingClientRect();
      setPickerPos({
        top: rect.bottom + window.scrollY + 4,
        left: rect.right + window.scrollX,
      });
    }
    setPickerOpen((p) => !p);
  }, []);

  const handleAssign = useCallback(
    (channelId: number) => {
      assign({ channelId });
      setPickerOpen(false);
    },
    [assign]
  );

  // Close on outside click. Deferred so the click that opened the picker
  // (which fires after the pointerup that triggers onPress) doesn't
  // immediately close it again.
  useEffect(() => {
    if (!pickerOpen) return;
    const handler = () => setPickerOpen(false);
    const timer = setTimeout(() => document.addEventListener('click', handler), 0);
    return () => {
      clearTimeout(timer);
      document.removeEventListener('click', handler);
    };
  }, [pickerOpen]);

  return (
    <TableRow>
      <TableRowCell className={styles.fileNameCell} title={file.filePath}>
        <button className={styles.fileNameBtn} onClick={() => setPlayerOpen(true)}>
          {file.fileName}
        </button>
      </TableRowCell>

      {playerOpen ? ReactDOM.createPortal(
        <div className={styles.playerOverlay} onClick={() => setPlayerOpen(false)}>
          <div className={styles.playerContainer} onClick={(e) => e.stopPropagation()}>
            <button className={styles.playerClose} onClick={() => setPlayerOpen(false)}>✕</button>
            <video
              className={styles.playerVideo}
              src={`/api/v1/unmatchedfile/${file.id}/stream?apikey=${window.Streamarr.apiKey}`}
              controls
              autoPlay
            />
          </div>
        </div>,
        document.body
      ) : null}

      <TableRowCell className={styles.reasonCell}>
        {unmatchedFileReasonLabel[file.reason] ?? 'Unknown'}
      </TableRowCell>

      <TableRowCell className={styles.sizeCell}>
        {formatBytes(file.fileSize)}
      </TableRowCell>

      <TableRowCell className={styles.dateCell}>
        {formatDate(file.dateFound)}
      </TableRowCell>

      <TableRowCell className={styles.actionsCell}>
        <div className={styles.actionButtons}>
          <div ref={buttonRef}>
            <IconButton
              name={icons.EXTERNAL_LINK}
              size={12}
              title="Assign to channel"
              isDisabled={isAssigning || channels.length === 0}
              onPress={handleOpenPicker}
            />
          </div>

          {pickerOpen ? ReactDOM.createPortal(
            <div
              className={styles.channelPicker}
              style={{ position: 'absolute', top: pickerPos.top, left: pickerPos.left, transform: 'translateX(-100%)' }}
              onClick={(e) => e.stopPropagation()}
            >
              {channels.map((ch) => (
                <button
                  key={ch.id}
                  type="button"
                  className={styles.channelOption}
                  onClick={() => handleAssign(ch.id)}
                >
                  {ch.title}
                </button>
              ))}
            </div>,
            document.body
          ) : null}

          <IconButton
            name={icons.REMOVE}
            size={12}
            title="Dismiss"
            isDisabled={isDismissing}
            onPress={() => dismiss()}
          />
        </div>
      </TableRowCell>
    </TableRow>
  );
}

interface CreatorUnmatchedSectionProps {
  creatorId: number;
  channels: Channel[];
}

function CreatorUnmatchedSection({ creatorId, channels }: CreatorUnmatchedSectionProps) {
  const { data: files, isFetched, isError } = useUnmatchedFilesByCreator(creatorId);
  const [expanded, setExpanded] = useState(true);

  const handleToggle = useCallback(() => {
    setExpanded((prev) => !prev);
  }, []);

  if (!isFetched || isError || files.length === 0) {
    return null;
  }

  return (
    <div className={styles.section}>
      <div className={styles.header} onClick={handleToggle}>
        <span className={`${styles.chevron} ${expanded ? '' : styles.chevronCollapsed}`}>
          ▼
        </span>

        <span className={styles.sectionLabel}>Unmatched Files</span>

        <span className={styles.count}>
          {files.length} file{files.length !== 1 ? 's' : ''}
        </span>
      </div>

      {expanded ? (
        <div className={styles.body}>
          <table className={styles.fixedTable}>
            <thead>
              <tr>
                <th className={styles.thCell}>File</th>
                <th className={styles.thCell}>Reason</th>
                <th className={styles.thCell}>Size</th>
                <th className={styles.thCell}>Date</th>
                <th className={styles.thCell} />
              </tr>
            </thead>
            <tbody>
              {files.map((file) => (
                <UnmatchedFileRow key={file.id} file={file} channels={channels} />
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </div>
  );
}

export default CreatorUnmatchedSection;
