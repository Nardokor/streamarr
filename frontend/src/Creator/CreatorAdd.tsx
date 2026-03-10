import React, { useCallback, useState } from 'react';
import { useHistory } from 'react-router';
import Button from 'Components/Link/Button';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import { kinds } from 'Helpers/Props';
import AddCreatorModal from './AddCreatorModal';
import useCreators from './useCreators';
import styles from './CreatorAdd.css';

function CreatorAdd() {
  const history = useHistory();
  const [isModalOpen, setIsModalOpen] = useState(false);

  const { data: creators } = useCreators();
  const hasCreators = (creators ?? []).length > 0;

  const handleAddPress = useCallback(() => {
    setIsModalOpen(true);
  }, []);

  const handleModalClose = useCallback(() => {
    setIsModalOpen(false);
  }, []);

  const handleCreatorAdded = useCallback(() => {
    history.push('/creator');
  }, [history]);

  return (
    <PageContent title="Add New Creator">
      <PageContentBody>
        <div className={styles.message}>
          <Button kind={kinds.PRIMARY} onPress={handleAddPress}>
            Add New Creator
          </Button>
        </div>

        {!hasCreators ? (
          <div className={styles.message}>
            <div className={styles.noCreatorsText}>
              No creators have been added yet
            </div>

            <div>
              <Button to="/add/import" kind={kinds.PRIMARY}>
                Import Existing Library
              </Button>
            </div>
          </div>
        ) : null}
      </PageContentBody>

      <AddCreatorModal
        isOpen={isModalOpen}
        onModalClose={handleModalClose}
        onCreatorAdded={handleCreatorAdded}
      />
    </PageContent>
  );
}

export default CreatorAdd;
