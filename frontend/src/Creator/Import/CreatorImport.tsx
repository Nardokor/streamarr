import React, { useCallback, useState } from 'react';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { ImportLibraryResult, useGetImportableFolders, useImportLibrary } from 'Settings/Import/useImport';
import styles from './CreatorImport.css';

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

function CreatorImport() {
  const [rootPath, setRootPath] = useState('');
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const { scanFolders, isScanning, folders, scanError } = useGetImportableFolders();
  const { importLibrary, isImporting, result, importError } = useImportLibrary();

  const handleScan = useCallback(() => {
    if (!rootPath.trim()) return;
    setSelected(new Set());
    scanFolders({ rootPath: rootPath.trim() });
  }, [rootPath, scanFolders]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') handleScan();
    },
    [handleScan]
  );

  const handleToggle = useCallback((folderName: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(folderName)) {
        next.delete(folderName);
      } else {
        next.add(folderName);
      }
      return next;
    });
  }, []);

  const handleSelectAll = useCallback(() => {
    setSelected(new Set(folders.map((f) => f.folderName)));
  }, [folders]);

  const handleSelectNone = useCallback(() => {
    setSelected(new Set());
  }, []);

  const handleImport = useCallback(() => {
    if (selected.size === 0) return;
    importLibrary({ rootPath: rootPath.trim(), folderNames: Array.from(selected) });
  }, [rootPath, selected, importLibrary]);

  const hasFolders = folders.length > 0;
  const hasScanned = hasFolders || scanError != null;

  return (
    <PageContent title="Import Library">
      <PageContentBody>
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Step 1 — Select root folder</h2>

          <div className={styles.pathRow}>
            <input
              className={styles.pathInput}
              type="text"
              value={rootPath}
              onChange={(e) => setRootPath(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder="/path/to/your/media/library"
              disabled={isScanning}
            />

            <button
              className={styles.actionButton}
              type="button"
              disabled={!rootPath.trim() || isScanning}
              onClick={handleScan}
            >
              {isScanning ? 'Scanning…' : 'Scan'}
            </button>
          </div>

          {scanError ? (
            <p className={styles.error}>Could not scan that path. Check that it exists and is readable.</p>
          ) : null}
        </section>

        {hasScanned ? (
          <section className={styles.section}>
            <h2 className={styles.sectionTitle}>Step 2 — Select folders to import</h2>

            {hasFolders ? (
              <>
                <div className={styles.folderControls}>
                  <button className={styles.linkButton} type="button" onClick={handleSelectAll}>
                    Select all
                  </button>

                  <span className={styles.separator}>/</span>

                  <button className={styles.linkButton} type="button" onClick={handleSelectNone}>
                    Select none
                  </button>

                  <span className={styles.selectedCount}>
                    {selected.size} of {folders.length} selected
                  </span>
                </div>

                <ul className={styles.folderList}>
                  {folders.map((folder) => (
                    <li key={folder.folderName} className={styles.folderItem}>
                      <label className={styles.folderLabel}>
                        <input
                          type="checkbox"
                          checked={selected.has(folder.folderName)}
                          onChange={() => handleToggle(folder.folderName)}
                        />

                        <span className={styles.folderName}>{folder.folderName}</span>
                        <span className={styles.folderPath}>{folder.path}</span>
                      </label>
                    </li>
                  ))}
                </ul>

                <div className={styles.importActions}>
                  <button
                    className={styles.actionButton}
                    type="button"
                    disabled={selected.size === 0 || isImporting}
                    onClick={handleImport}
                  >
                    {isImporting ? 'Importing…' : `Import ${selected.size > 0 ? `${selected.size} folder${selected.size !== 1 ? 's' : ''}` : ''}`}
                  </button>

                  {importError ? (
                    <span className={styles.error}>Import failed — check the logs for details.</span>
                  ) : null}
                </div>
              </>
            ) : (
              <p className={styles.empty}>
                No unimported folders found in that path. All subdirectories may already be creators.
              </p>
            )}

            {result ? <ResultSummary result={result} /> : null}
          </section>
        ) : null}
      </PageContentBody>
    </PageContent>
  );
}

export default CreatorImport;
