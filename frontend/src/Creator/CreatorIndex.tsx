import React, { useCallback, useMemo, useState } from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Menu from 'Components/Menu/Menu';
import SelectedMenuItem from 'Components/Menu/SelectedMenuItem';
import SortMenu from 'Components/Menu/SortMenu';
import SortMenuItem from 'Components/Menu/SortMenuItem';
import ToolbarMenuButton from 'Components/Menu/ToolbarMenuButton';
import ViewMenu from 'Components/Menu/ViewMenu';
import ViewMenuItem from 'Components/Menu/ViewMenuItem';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import Creator from 'typings/Creator';
import CreatorPoster from './CreatorPoster';
import CreatorRow from './CreatorRow';
import useCreators from './useCreators';
import styles from './CreatorIndex.css';

type ViewMode = 'poster' | 'table';
type FilterKey = 'all' | 'monitored' | 'unmonitored';
type SortKey = 'title' | 'added' | 'monitored';

const columns: Column[] = [
  { name: 'thumbnail', label: '', isVisible: true },
  { name: 'title', label: 'Creator', isVisible: true },
  { name: 'path', label: 'Path', isVisible: true },
  { name: 'monitored', label: 'Monitored', isVisible: true },
  { name: 'actions', label: '', isVisible: true },
];

function readViewPref(): ViewMode {
  try {
    const v = localStorage.getItem('creatorView');
    if (v === 'table' || v === 'poster') {
      return v;
    }
  } catch {
    // ignore
  }
  return 'poster';
}

function sortCreators(
  list: Creator[],
  key: SortKey,
  dir: SortDirection
): Creator[] {
  return [...list].sort((a, b) => {
    let cmp = 0;
    if (key === 'title') {
      cmp = a.title.localeCompare(b.title);
    } else if (key === 'added') {
      cmp = new Date(a.added).getTime() - new Date(b.added).getTime();
    } else if (key === 'monitored') {
      cmp = (b.monitored ? 1 : 0) - (a.monitored ? 1 : 0);
    }
    return dir === 'ascending' ? cmp : -cmp;
  });
}

function CreatorIndex() {
  const { data: creators, isLoading, error } = useCreators();

  const [view, setView] = useState<ViewMode>(readViewPref);
  const [sortKey, setSortKey] = useState<SortKey>('title');
  const [sortDirection, setSortDirection] = useState<SortDirection>('ascending');
  const [filterKey, setFilterKey] = useState<FilterKey>('all');

  const handleViewChange = useCallback((v: string) => {
    const mode = v as ViewMode;
    setView(mode);
    try {
      localStorage.setItem('creatorView', mode);
    } catch {
      // ignore
    }
  }, []);

  const handleSortPress = useCallback(
    (key: string) => {
      if (key === sortKey) {
        setSortDirection((d) => (d === 'ascending' ? 'descending' : 'ascending'));
      } else {
        setSortKey(key as SortKey);
        setSortDirection('ascending');
      }
    },
    [sortKey]
  );

  const handleFilterChange = useCallback((key: string) => {
    setFilterKey(key as FilterKey);
  }, []);

  const displayed = useMemo(() => {
    let list = creators;
    if (filterKey === 'monitored') {
      list = list.filter((c) => c.monitored);
    } else if (filterKey === 'unmonitored') {
      list = list.filter((c) => !c.monitored);
    }
    return sortCreators(list, sortKey, sortDirection);
  }, [creators, filterKey, sortKey, sortDirection]);

  const noCreators = !creators.length && !isLoading && !error;

  return (
    <PageContent title="Creators">
      <PageToolbar>
        <PageToolbarSection>
          <ViewMenu>
            <ViewMenuItem
              name="poster"
              selectedView={view}
              onPress={handleViewChange}
            >
              Poster
            </ViewMenuItem>
            <ViewMenuItem
              name="table"
              selectedView={view}
              onPress={handleViewChange}
            >
              Table
            </ViewMenuItem>
          </ViewMenu>

          <SortMenu>
            <SortMenuItem
              name="title"
              sortKey={sortKey}
              sortDirection={sortDirection}
              onPress={handleSortPress}
            >
              Title
            </SortMenuItem>
            <SortMenuItem
              name="added"
              sortKey={sortKey}
              sortDirection={sortDirection}
              onPress={handleSortPress}
            >
              Date Added
            </SortMenuItem>
            <SortMenuItem
              name="monitored"
              sortKey={sortKey}
              sortDirection={sortDirection}
              onPress={handleSortPress}
            >
              Monitored
            </SortMenuItem>
          </SortMenu>

          <Menu>
            <ToolbarMenuButton
              iconName={icons.FILTER}
              text="Filter"
              showIndicator={filterKey !== 'all'}
            />
            <SelectedMenuItem
              name="all"
              isSelected={filterKey === 'all'}
              onPress={handleFilterChange}
            >
              All
            </SelectedMenuItem>
            <SelectedMenuItem
              name="monitored"
              isSelected={filterKey === 'monitored'}
              onPress={handleFilterChange}
            >
              Monitored
            </SelectedMenuItem>
            <SelectedMenuItem
              name="unmonitored"
              isSelected={filterKey === 'unmonitored'}
              onPress={handleFilterChange}
            >
              Unmonitored
            </SelectedMenuItem>
          </Menu>
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
            No creators added yet. Use &ldquo;Creators &rsaquo; Add New&rdquo;
            in the sidebar to get started.
          </Alert>
        ) : null}

        {!isLoading && !error && displayed.length > 0 && view === 'poster' ? (
          <div className={styles.posterGrid}>
            {displayed.map((creator) => (
              <CreatorPoster
                key={creator.id}
                creator={creator}
                channelCount={0}
              />
            ))}
          </div>
        ) : null}

        {!isLoading && !error && displayed.length > 0 && view === 'table' ? (
          <Table columns={columns}>
            <TableBody>
              {displayed.map((creator) => (
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
