import React, { useCallback, useState } from 'react';
import FieldSet from 'Components/FieldSet';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { ImportLibraryResult, useImportLibrary } from './useImport';
import styles from './ImportSettings.css';

function ResultSummary({ result }: { result: ImportLibraryResult }) {
  return (
    <div className={styles.result}>
      <h3 className={styles.resultTitle}>Import Complete</h3>

      <table className={styles.resultTable}>
        <tbody>
          <tr>
            <td>Creators created</td>
            <td className={styles.resultValue}>{result.creatorsCreated}</td>
          </tr>

          <tr>
            <td>Creators matched (existing)</td>
            <td className={styles.resultValue}>{result.creatorsMatched}</td>
          </tr>

          <tr>
            <td>Channels created</td>
            <td className={styles.resultValue}>{result.channelsCreated}</td>
          </tr>

          <tr>
            <td>Content linked to files</td>
            <td className={styles.resultValue}>{result.contentLinked}</td>
          </tr>

          <tr>
            <td>Already linked (skipped)</td>
            <td className={styles.resultValue}>{result.contentAlreadyLinked}</td>
          </tr>

          <tr>
            <td>Files without a YouTube match</td>
            <td className={styles.resultValue}>{result.filesNotMatched}</td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}

function ImportSettings() {
  const [rootPath, setRootPath] = useState('');
  const { importLibrary, isImporting, result, importError } = useImportLibrary();

  const handleImport = useCallback(() => {
    if (!rootPath.trim()) {
      return;
    }

    importLibrary({ rootPath: rootPath.trim() });
  }, [rootPath, importLibrary]);

  return (
    <PageContent title="Import Library">
      <PageContentBody>
        <FieldSet legend="Import Existing Library">
          <p className={styles.description}>
            Point Streamarr at a folder of existing creator content. Each subdirectory becomes a
            creator. Videos with a YouTube ID in the filename (e.g.{' '}
            <code>Title [dQw4w9WgXcQ].mp4</code>) are matched against YouTube to discover the
            channel, and marked as already downloaded.
          </p>

          <div className={styles.formRow}>
            <label className={styles.label} htmlFor="rootPath">
              Library root path
            </label>

            <input
              id="rootPath"
              className={styles.pathInput}
              type="text"
              value={rootPath}
              onChange={(e) => setRootPath(e.target.value)}
              placeholder="/path/to/your/media/library"
              disabled={isImporting}
            />
          </div>

          <div className={styles.actions}>
            <button
              className={styles.importButton}
              type="button"
              disabled={!rootPath.trim() || isImporting}
              onClick={handleImport}
            >
              {isImporting ? 'Importing…' : 'Import'}
            </button>
          </div>

          {importError ? (
            <div className={styles.error}>
              Import failed. Check the Streamarr logs for details.
            </div>
          ) : null}

          {result ? <ResultSummary result={result} /> : null}
        </FieldSet>
      </PageContentBody>
    </PageContent>
  );
}

export default ImportSettings;
