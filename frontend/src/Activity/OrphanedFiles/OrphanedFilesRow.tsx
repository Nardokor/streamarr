import React, { useCallback } from 'react';
import Icon from 'Components/Icon';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { icons } from 'Helpers/Props';
import formatBytes from 'Utilities/Number/formatBytes';
import { OrphanedFile, useDeleteOrphanedFile } from './useOrphanedFiles';
import styles from './OrphanedFiles.css';

interface OrphanedFilesRowProps {
  file: OrphanedFile;
}

function formatAge(lastWriteUtc: string): string {
  const seconds = Math.max(
    0,
    Math.floor((Date.now() - new Date(lastWriteUtc).getTime()) / 1000)
  );

  if (seconds < 60) return `${seconds}s ago`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export default function OrphanedFilesRow({ file }: OrphanedFilesRowProps) {
  const { deleteFile, isDeleting } = useDeleteOrphanedFile(file.path);

  const handleDelete = useCallback(() => {
    deleteFile(undefined);
  }, [deleteFile]);

  return (
    <TableRow>
      <TableRowCell className={styles.fileName}>{file.fileName}</TableRowCell>

      <TableRowCell>{file.creatorTitle}</TableRowCell>

      <TableRowCell>{formatBytes(file.size)}</TableRowCell>

      <TableRowCell>
        {file.stale ? (
          <span className={styles.stale}>
            <Icon name={icons.WARNING} size={12} />{' '}
            {formatAge(file.lastWriteUtc)}
          </span>
        ) : (
          formatAge(file.lastWriteUtc)
        )}
      </TableRowCell>

      <TableRowCell className={styles.deleteCell}>
        <button
          className={styles.deleteBtn}
          disabled={isDeleting}
          title="Delete file"
          type="button"
          onClick={handleDelete}
        >
          <Icon name={icons.DELETE} size={14} />
        </button>
      </TableRowCell>
    </TableRow>
  );
}
