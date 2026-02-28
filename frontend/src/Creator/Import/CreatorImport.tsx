import React, { useCallback, useEffect, useState } from 'react';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import useRootFolders from 'RootFolder/useRootFolders';
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
  const [selectedRoot, setSelectedRoot] = useState<string | null>(null);
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const { data: rootFolders, isLoading: isLoadingRoots } = useRootFolders();
  const { scanFolders, isScanning, folders, scanError } = useGetImportableFolders();
  const { importLibrary, isImporting, result, importError } = useImportLibrary();

  const handleSelectRoot = useCallback(
    (path: string) => {
      setSelectedRoot(path);
      setSelected(new Set());
      scanFolders({ rootPath: path });
    },
    [scanFolders]
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
    if (selected.size === 0 || !selectedRoot) return;
    importLibrary({ rootPath: selectedRoot, folderNames: Array.from(selected) });
  }, [selectedRoot, selected, importLibrary]);

  // Reset folder selection when scan results change
  useEffect(() => {
    setSelected(new Set());
  }, [folders]);

  const hasFolders = folders.length > 0;
  const hasScanned = selectedRoot !== null && !isScanning && (hasFolders || scanError != null);

  return (
    <PageContent title="Import Library">
      <PageContentBody>
        <section className={styles.section}>
          <h2 className={styles.sectionTitle}>Step 1 — Choose a root folder</h2>

          {isLoadingRoots ? (
            <p className={styles.empty}>Loading root folders…</p>
          ) : rootFolders.length === 0 ? (
            <p className={styles.empty}>
              No root folders configured. Add one under Settings &rsaquo; Media Management first.
            </p>
          ) : (
            <ul className={styles.rootList}>
              {rootFolders.map((rf) => (
                <li key={rf.id}>
                  <button
                    type="button"
                    className={
                      selectedRoot === rf.path
                        ? `${styles.rootItem} ${styles.rootItemActive}`
                        : styles.rootItem
                    }
                    onClick={() => handleSelectRoot(rf.path)}
                    disabled={isScanning}
                  >
                    <span className={styles.rootItemPath}>{rf.path}</span>
                    {selectedRoot === rf.path && isScanning ? (
                      <span className={styles.rootItemStatus}>Scanning…</span>
                    ) : null}
                  </button>
                </li>
              ))}
            </ul>
          )}

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
