import React, { useCallback, useState } from 'react';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import FileBrowserModal from 'Components/FileBrowser/FileBrowserModal';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { icons, kinds, sizes } from 'Helpers/Props';
import RootFolders from 'RootFolder/RootFolders';
import useRootFolders, { useAddRootFolder } from 'RootFolder/useRootFolders';
import { InputChanged } from 'typings/inputs';
import styles from './CreatorImport.css';

function CreatorImport() {
  const { isFetching, isFetched, error } = useRootFolders();
  const { addRootFolder, isAdding, addError } = useAddRootFolder();
  const { data: rootFolders } = useRootFolders();

  const [isFileBrowserOpen, setIsFileBrowserOpen] = useState(false);

  const hasRootFolders = rootFolders.length > 0;

  const handleChooseFolderPress = useCallback(() => {
    setIsFileBrowserOpen(true);
  }, []);

  const handleFileBrowserClose = useCallback(() => {
    setIsFileBrowserOpen(false);
  }, []);

  const handleNewRootFolderSelect = useCallback(
    ({ value }: InputChanged<string>) => {
      addRootFolder({ path: value });
      setIsFileBrowserOpen(false);
    },
    [addRootFolder]
  );

  return (
    <PageContent title="Library Import">
      <PageContentBody>
        {isFetching && !isFetched ? <LoadingIndicator /> : null}

        {!isFetching && error ? (
          <Alert kind={kinds.DANGER}>Unable to load root folders.</Alert>
        ) : null}

        {isFetched ? (
          <div>
            <div className={styles.header}>
              Import creators you already have
            </div>

            <div className={styles.tips}>
              Some tips to ensure the import goes smoothly:

              <ul>
                <li className={styles.tip}>
                  Point Streamarr to the folder containing all of your creators, not a specific one.
                  {' '}eg. <code className={styles.code}>/media/creators</code> and not{' '}
                  <code className={styles.code}>/media/creators/MrBeast</code>
                </li>

                <li className={styles.tip}>
                  Each creator must be in their own subfolder within the root folder.
                </li>

                <li className={styles.tip}>
                  This is only for existing organized libraries, not unsorted files.
                </li>
              </ul>
            </div>

            {hasRootFolders ? (
              <div className={styles.recentFolders}>
                <FieldSet legend="Root Folders">
                  <RootFolders />
                </FieldSet>
              </div>
            ) : null}

            {!isAdding && addError ? (
              <Alert className={styles.addErrorAlert} kind={kinds.DANGER}>
                Unable to add root folder. Check that the path exists and is accessible.
              </Alert>
            ) : null}

            <div className={hasRootFolders ? undefined : styles.startImport}>
              <Button
                kind={kinds.PRIMARY}
                size={sizes.LARGE}
                onPress={handleChooseFolderPress}
              >
                <Icon className={styles.importButtonIcon} name={icons.DRIVE} />

                {hasRootFolders ? 'Choose another folder' : 'Start import'}
              </Button>
            </div>
          </div>
        ) : null}
      </PageContentBody>

      <FileBrowserModal
        isOpen={isFileBrowserOpen}
        name="rootFolderPath"
        value=""
        onChange={handleNewRootFolderSelect}
        onModalClose={handleFileBrowserClose}
      />
    </PageContent>
  );
}

export default CreatorImport;
