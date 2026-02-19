import React, { useCallback, useState } from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds } from 'Helpers/Props';
import AddCreatorModal from './AddCreatorModal';
import CreatorRow from './CreatorRow';
import useCreators from './useCreators';

const columns: Column[] = [
  {
    name: 'thumbnail',
    label: '',
    isVisible: true,
  },
  {
    name: 'title',
    label: 'Creator',
    isVisible: true,
  },
  {
    name: 'path',
    label: 'Path',
    isVisible: true,
  },
  {
    name: 'monitored',
    label: 'Monitored',
    isVisible: true,
  },
  {
    name: 'actions',
    label: '',
    isVisible: true,
  },
];

function CreatorIndex() {
  const { data: creators, isLoading, error, refetch } = useCreators();

  const [isAddModalOpen, setIsAddModalOpen] = useState(false);

  const handleAddPress = useCallback(() => {
    setIsAddModalOpen(true);
  }, []);

  const handleAddModalClose = useCallback(() => {
    setIsAddModalOpen(false);
  }, []);

  const handleCreatorAdded = useCallback(() => {
    refetch();
  }, [refetch]);

  const noCreators = !creators.length && !isLoading && !error;
  const hasCreators = !!creators.length;

  return (
    <PageContent title="Creators">
      <PageToolbar>
        <PageToolbarSection>
          <PageToolbarButton
            label="Add Creator"
            iconName={icons.ADD}
            onPress={handleAddPress}
          />
        </PageToolbarSection>
      </PageToolbar>

      <PageContentBody>
        {isLoading ? <LoadingIndicator /> : null}

        {!isLoading && !!error ? (
          <Alert kind={kinds.DANGER}>
            Failed to load creators. Check that Streamarr is running correctly.
          </Alert>
        ) : null}

        {noCreators ? (
          <Alert kind={kinds.INFO}>
            No creators added yet. Click &ldquo;Add Creator&rdquo; to get
            started.
          </Alert>
        ) : null}

        {hasCreators ? (
          <Table columns={columns}>
            <TableBody>
              {creators.map((creator) => (
                <CreatorRow key={creator.id} creator={creator} />
              ))}
            </TableBody>
          </Table>
        ) : null}
      </PageContentBody>

      <AddCreatorModal
        isOpen={isAddModalOpen}
        onModalClose={handleAddModalClose}
        onCreatorAdded={handleCreatorAdded}
      />
    </PageContent>
  );
}

export default CreatorIndex;
