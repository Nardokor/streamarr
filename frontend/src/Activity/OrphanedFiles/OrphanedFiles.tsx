import React from 'react';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import OrphanedFilesRow from './OrphanedFilesRow';
import { useOrphanedFiles } from './useOrphanedFiles';
import styles from './OrphanedFiles.css';

const columns: Column[] = [
  {
    name: 'fileName',
    label: 'File',
    isVisible: true,
  },
  {
    name: 'creatorTitle',
    label: 'Creator',
    isVisible: true,
  },
  {
    name: 'size',
    label: 'Size',
    isVisible: true,
  },
  {
    name: 'age',
    label: 'Last Modified',
    isVisible: true,
  },
  {
    name: 'actions',
    label: '',
    isVisible: true,
  },
];

function OrphanedFiles() {
  const { data: files, isLoading } = useOrphanedFiles();

  if (isLoading) {
    return (
      <PageContent title="Orphaned Files">
        <PageContentBody>
          <LoadingIndicator />
        </PageContentBody>
      </PageContent>
    );
  }

  return (
    <PageContent title="Orphaned Files">
      <PageContentBody>
        <p className={styles.helpText}>
          Partial and fragment files left behind by yt-dlp (.part, .ytdl, format
          fragments). Files flagged as stale haven&apos;t been modified in over
          2 hours and are unlikely to belong to an active download — review
          before deleting.
        </p>

        {files.length > 0 ? (
          <Table columns={columns}>
            <TableBody>
              {files.map((file) => (
                <OrphanedFilesRow key={file.path} file={file} />
              ))}
            </TableBody>
          </Table>
        ) : (
          <div className={styles.empty}>No orphaned files found</div>
        )}
      </PageContentBody>
    </PageContent>
  );
}

export default OrphanedFiles;
