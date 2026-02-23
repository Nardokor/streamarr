import React, { useCallback } from 'react';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import useApiQuery from 'Helpers/Hooks/useApiQuery';
import Content from 'typings/Content';
import {
  buildPlatformUrl,
  formatDate,
  formatDuration,
  getContentTypeLabel,
  getStatusLabel,
} from './creatorUtils';
import styles from './ContentDetailModal.css';

interface Props {
  contentId: number | null;
  channelPlatform: string;
  onClose: () => void;
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  if (bytes < 1024 * 1024 * 1024) {
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
}

function ContentDetailModal({ contentId, channelPlatform, onClose }: Props) {
  const { data: content } = useApiQuery<Content>({
    path: `/content/${contentId}`,
    queryOptions: { enabled: contentId != null },
  });

  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  const videoUrl = content
    ? buildPlatformUrl(channelPlatform, content.platformContentId)
    : null;
  const status = content ? getStatusLabel(content) : null;
  const typeLabel = content ? getContentTypeLabel(content.contentType) : null;

  return (
    <Modal isOpen={contentId != null} onModalClose={handleClose}>
      <ModalContent onModalClose={handleClose}>
        <ModalHeader>
          {content?.title ?? 'Loading…'}
        </ModalHeader>

        <ModalBody>
          {content ? (
            <div className={styles.detail}>
              {content.thumbnailUrl ? (
                <img
                  className={styles.thumbnail}
                  src={content.thumbnailUrl}
                  alt={content.title}
                />
              ) : null}

              <table className={styles.table}>
                <tbody>
                  {typeLabel ? (
                    <tr>
                      <th>Type</th>
                      <td>{typeLabel}</td>
                    </tr>
                  ) : null}

                  {status ? (
                    <tr>
                      <th>Status</th>
                      <td>{status.text}</td>
                    </tr>
                  ) : null}

                  <tr>
                    <th>Published</th>
                    <td>{content.airDateUtc ? formatDate(content.airDateUtc) : '—'}</td>
                  </tr>

                  {content.duration ? (
                    <tr>
                      <th>Duration</th>
                      <td>{formatDuration(content.duration)}</td>
                    </tr>
                  ) : null}

                  <tr>
                    <th>Monitored</th>
                    <td>{content.monitored ? 'Yes' : 'No'}</td>
                  </tr>

                  {content.fileRelativePath ? (
                    <tr>
                      <th>File</th>
                      <td className={styles.filePath}>{content.fileRelativePath}</td>
                    </tr>
                  ) : null}

                  {content.fileSize != null ? (
                    <tr>
                      <th>Size</th>
                      <td>{formatBytes(content.fileSize)}</td>
                    </tr>
                  ) : null}

                  {content.description ? (
                    <tr>
                      <th>Description</th>
                      <td className={styles.description}>{content.description}</td>
                    </tr>
                  ) : null}
                </tbody>
              </table>
            </div>
          ) : (
            <div className={styles.loading}>Loading…</div>
          )}
        </ModalBody>

        <ModalFooter>
          {videoUrl ? (
            <a
              className={styles.externalLink}
              href={videoUrl}
              target="_blank"
              rel="noreferrer"
            >
              Open on {channelPlatform === 'youTube' ? 'YouTube' : 'Twitch'}
            </a>
          ) : null}
          <Button onPress={handleClose}>Close</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default ContentDetailModal;
