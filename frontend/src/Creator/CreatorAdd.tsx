import React, { useCallback } from 'react';
import { useHistory } from 'react-router';
import AddCreatorModal from './AddCreatorModal';

function CreatorAdd() {
  const history = useHistory();

  const handleModalClose = useCallback(() => {
    history.push('/creator');
  }, [history]);

  return (
    <AddCreatorModal
      isOpen={true}
      onModalClose={handleModalClose}
      onCreatorAdded={handleModalClose}
    />
  );
}

export default CreatorAdd;
