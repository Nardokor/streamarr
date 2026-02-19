import React from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { kinds } from 'Helpers/Props';
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
  const { data: creators, isLoading, error } = useCreators();

  const noCreators = !creators.length && !isLoading && !error;
  const hasCreators = !!creators.length;

  return (
    <PageContent title="Creators">
      <PageContentBody>
        {isLoading ? <LoadingIndicator /> : null}

        {!isLoading && !!error ? (
          <Alert kind={kinds.DANGER}>
            Failed to load creators. Check that Streamarr is running correctly.
          </Alert>
        ) : null}

        {noCreators ? (
          <Alert kind={kinds.INFO}>
            No creators added yet. Use &ldquo;Creators &rsaquo; Add New&rdquo;
            in the sidebar to get started.
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
    </PageContent>
  );
}

export default CreatorIndex;
