import React, { useCallback, useEffect, useState } from 'react';
import { useParams } from 'react-router';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableHeader from 'Components/Table/TableHeader';
import TableHeaderCell from 'Components/Table/TableHeaderCell';
import TableRow from 'Components/Table/TableRow';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import { kinds } from 'Helpers/Props';
import useRootFolders from 'RootFolder/useRootFolders';
import { ImportLibraryResult, useGetImportableFolders, useImportLibrary } from 'Settings/Import/useImport';
import { SelectStateInputProps } from 'typings/props';
import styles from './CreatorImportTable.css';

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

function CreatorImportTable() {
  const { rootFolderId } = useParams<{ rootFolderId: string }>();
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const { data: rootFolders } = useRootFolders();
  const rootFolder = rootFolders.find((rf) => rf.id === parseInt(rootFolderId, 10));
  const rootPath = rootFolder?.path ?? '';

  const { scanFolders, isScanning, folders, scanError } = useGetImportableFolders();
  const { importLibrary, isImporting, result, importError } = useImportLibrary();

  useEffect(() => {
    if (rootPath) {
      scanFolders({ rootPath });
    }
  }, [rootPath, scanFolders]);

  useEffect(() => {
    setSelected(new Set());
  }, [folders]);

  const hasFolders = folders.length > 0;
  const allSelected = hasFolders && selected.size === folders.length;
  const allUnselected = selected.size === 0;

  const handleSelectedChange = useCallback(
    ({ id, value }: SelectStateInputProps<string>) => {
      setSelected((prev) => {
        const next = new Set(prev);
        if (value) {
          next.add(id);
        } else {
          next.delete(id);
        }
        return next;
      });
    },
    []
  );

  const handleSelectAll = useCallback(() => {
    setSelected(new Set(folders.map((f) => f.folderName)));
  }, [folders]);

  const handleSelectNone = useCallback(() => {
    setSelected(new Set());
  }, []);

  const handleImport = useCallback(() => {
    if (selected.size === 0 || !rootPath) return;
    importLibrary({ rootPath, folderNames: Array.from(selected) });
  }, [rootPath, selected, importLibrary]);

  return (
    <PageContent title={rootPath ? `Import — ${rootPath}` : 'Import'}>
      <PageContentBody>
        {isScanning ? (
          <div className={styles.scanning}>Scanning {rootPath}…</div>
        ) : null}

        {!isScanning && scanError ? (
          <div className={styles.error}>
            Could not scan that path. Check that it exists and is readable.
          </div>
        ) : null}

        {!isScanning && !scanError && hasFolders ? (
          <>
            <div className={styles.folderControls}>
              <Button onPress={handleSelectAll}>Select all</Button>

              <Button onPress={handleSelectNone}>Select none</Button>

              <span className={styles.selectedCount}>
                {selected.size} of {folders.length} selected
              </span>
            </div>

            <Table
              selectAll={true}
              allSelected={allSelected}
              allUnselected={allUnselected}
              onSelectAllChange={({ value }) =>
                value ? handleSelectAll() : handleSelectNone()
              }
              columns={[]}
            >
              <TableHeader>
                <TableHeaderCell name="select" />
                <TableHeaderCell name="folder" label="Folder" />
                <TableHeaderCell name="path" label="Path" />
              </TableHeader>

              <TableBody>
                {folders.map((folder) => (
                  <TableRow key={folder.folderName}>
                    <TableSelectCell
                      id={folder.folderName}
                      isSelected={selected.has(folder.folderName)}
                      onSelectedChange={handleSelectedChange}
                    />

                    <TableRowCell className={styles.folderCell}>
                      {folder.folderName}
                    </TableRowCell>

                    <TableRowCell className={styles.pathCell}>
                      {folder.path}
                    </TableRowCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>

            <div className={styles.footer}>
              <SpinnerButton
                kind={kinds.PRIMARY}
                isDisabled={selected.size === 0}
                isSpinning={isImporting}
                onPress={handleImport}
              >
                Import {selected.size > 0
                  ? `${selected.size} folder${selected.size !== 1 ? 's' : ''}`
                  : ''}
              </SpinnerButton>

              {importError ? (
                <span className={styles.error}>
                  Import failed — check the logs for details.
                </span>
              ) : null}
            </div>
          </>
        ) : null}

        {!isScanning && !scanError && !hasFolders && rootPath ? (
          <div className={styles.empty}>
            No unimported folders found in {rootPath}. All subdirectories may already be creators.
          </div>
        ) : null}

        {result ? <ResultSummary result={result} /> : null}
      </PageContentBody>
    </PageContent>
  );
}

export default CreatorImportTable;
