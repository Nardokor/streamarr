import React, { useCallback, useMemo, useRef, useState } from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import Menu from 'Components/Menu/Menu';
import MenuContent from 'Components/Menu/MenuContent';
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
import { OnScroll } from 'Components/Scroller/Scroller';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import { icons, kinds } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import Creator from 'typings/Creator';
import CreatorPoster from './CreatorPoster';
import CreatorRow from './CreatorRow';
import useCreators, { CreatorStats, useCreatorStats } from './useCreators';
import styles from './CreatorIndex.css';

type ViewMode = 'poster' | 'table';
type FilterKey = 'all' | 'monitored' | 'unmonitored' | 'liveNow' | 'activeMembership' | 'missing';
type SortKey = 'title' | 'added' | 'monitored' | 'downloaded' | 'wanted' | 'missing';

const columns: Column[] = [
  { name: 'thumbnail', label: '', isVisible: true },
  { name: 'monitored', label: '', isVisible: true },
  { name: 'title', label: 'Creator', isVisible: true },
  { name: 'nextLive', label: 'Next Live', isVisible: true },
  { name: 'progress', label: 'Progress', isVisible: true },
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

const SESSION_KEY = 'creatorIndexState';

interface SessionState {
  sortKey: SortKey;
  sortDirection: SortDirection;
  filterKey: FilterKey;
  searchQuery: string;
  scrollTop: number;
}

function readSessionState(): Partial<SessionState> {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY);
    if (raw) {
      return JSON.parse(raw) as Partial<SessionState>;
    }
  } catch {
    // ignore
  }
  return {};
}

function writeSessionState(patch: Partial<SessionState>) {
  try {
    const current = readSessionState();
    sessionStorage.setItem(SESSION_KEY, JSON.stringify({ ...current, ...patch }));
  } catch {
    // ignore
  }
}

function sortCreators(
  list: Creator[],
  key: SortKey,
  dir: SortDirection,
  statsById: Map<number, CreatorStats>
): Creator[] {
  return [...list].sort((a, b) => {
    let cmp = 0;
    const sa = statsById.get(a.id);
    const sb = statsById.get(b.id);
    if (key === 'title') {
      cmp = a.title.localeCompare(b.title);
    } else if (key === 'added') {
      cmp = new Date(a.added).getTime() - new Date(b.added).getTime();
    } else if (key === 'monitored') {
      cmp = (b.monitored ? 1 : 0) - (a.monitored ? 1 : 0);
    } else if (key === 'downloaded') {
      cmp = (sa?.downloadedCount ?? 0) - (sb?.downloadedCount ?? 0);
    } else if (key === 'wanted') {
      cmp = (sa?.wantedCount ?? 0) - (sb?.wantedCount ?? 0);
    } else if (key === 'missing') {
      const ma = (sa?.wantedCount ?? 0) - (sa?.downloadedCount ?? 0);
      const mb = (sb?.wantedCount ?? 0) - (sb?.downloadedCount ?? 0);
      cmp = ma - mb;
    }
    return dir === 'ascending' ? cmp : -cmp;
  });
}

function CreatorIndex() {
  const { data: creators, isLoading, error } = useCreators();
  const { data: stats } = useCreatorStats();

  const statsById = useMemo(
    () => new Map(stats.map((s) => [s.creatorId, s])),
    [stats]
  );

  const session = useMemo(() => readSessionState(), []);
  const initialScrollTop = useRef(session.scrollTop ?? 0);

  const [view, setView] = useState<ViewMode>(readViewPref);
  const [sortKey, setSortKey] = useState<SortKey>(session.sortKey ?? 'title');
  const [sortDirection, setSortDirection] = useState<SortDirection>(session.sortDirection ?? 'ascending');
  const [filterKey, setFilterKey] = useState<FilterKey>(session.filterKey ?? 'all');
  const [searchQuery, setSearchQuery] = useState(session.searchQuery ?? '');

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
        setSortDirection((d) => {
          const next = d === 'ascending' ? 'descending' : 'ascending';
          writeSessionState({ sortKey: sortKey, sortDirection: next });
          return next;
        });
      } else {
        const nextDir: SortDirection = key === 'title' ? 'ascending' : 'descending';
        setSortKey(key as SortKey);
        setSortDirection(nextDir);
        writeSessionState({ sortKey: key as SortKey, sortDirection: nextDir });
      }
    },
    [sortKey]
  );

  const handleFilterChange = useCallback((key: string) => {
    setFilterKey(key as FilterKey);
    writeSessionState({ filterKey: key as FilterKey });
  }, []);

  const handleSearchChange = useCallback((query: string) => {
    setSearchQuery(query);
    writeSessionState({ searchQuery: query });
  }, []);

  const handleScroll = useCallback(({ scrollTop }: OnScroll) => {
    writeSessionState({ scrollTop });
  }, []);

  const hasActiveFilter = filterKey !== 'all';

  const displayed = useMemo(() => {
    let list = creators;
    if (filterKey === 'monitored') {
      list = list.filter((c) => c.monitored);
    } else if (filterKey === 'unmonitored') {
      list = list.filter((c) => !c.monitored);
    } else if (filterKey === 'liveNow') {
      list = list.filter((c) => statsById.get(c.id)?.isLiveNow === true);
    } else if (filterKey === 'activeMembership') {
      list = list.filter((c) => statsById.get(c.id)?.hasActiveMembership === true);
    } else if (filterKey === 'missing') {
      list = list.filter((c) => statsById.get(c.id)?.hasMissing === true);
    }
    if (searchQuery.trim()) {
      const q = searchQuery.trim().toLowerCase();
      list = list.filter((c) => c.title.toLowerCase().includes(q));
    }
    return sortCreators(list, sortKey, sortDirection, statsById);
  }, [creators, filterKey, searchQuery, sortKey, sortDirection, statsById]);

  const noCreators = !creators.length && !isLoading && !error;

  return (
    <PageContent title="Creators">
      <PageToolbar>
        <PageToolbarSection>
          <div className={styles.searchWrapper}>
            <input
              className={styles.searchInput}
              type="text"
              placeholder="Search creators…"
              value={searchQuery}
              onChange={(e) => handleSearchChange(e.target.value)}
            />
            {searchQuery ? (
              <button
                className={styles.clearBtn}
                type="button"
                onClick={() => handleSearchChange('')}
              >
                ✕
              </button>
            ) : null}
          </div>

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
              name="downloaded"
              sortKey={sortKey}
              sortDirection={sortDirection}
              onPress={handleSortPress}
            >
              Downloaded
            </SortMenuItem>
            <SortMenuItem
              name="wanted"
              sortKey={sortKey}
              sortDirection={sortDirection}
              onPress={handleSortPress}
            >
              Wanted
            </SortMenuItem>
            <SortMenuItem
              name="missing"
              sortKey={sortKey}
              sortDirection={sortDirection}
              onPress={handleSortPress}
            >
              Missing
            </SortMenuItem>
          </SortMenu>

          <Menu>
            <ToolbarMenuButton
              iconName={icons.FILTER}
              text="Filter"
              showIndicator={hasActiveFilter}
            />
            <MenuContent>
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
              <SelectedMenuItem
                name="liveNow"
                isSelected={filterKey === 'liveNow'}
                onPress={handleFilterChange}
              >
                Live Now
              </SelectedMenuItem>
              <SelectedMenuItem
                name="activeMembership"
                isSelected={filterKey === 'activeMembership'}
                onPress={handleFilterChange}
              >
                Active Membership
              </SelectedMenuItem>
              <SelectedMenuItem
                name="missing"
                isSelected={filterKey === 'missing'}
                onPress={handleFilterChange}
              >
                Missing Videos
              </SelectedMenuItem>
            </MenuContent>
          </Menu>
        </PageToolbarSection>
      </PageToolbar>

      <PageContentBody
        initialScrollTop={initialScrollTop.current}
        onScroll={handleScroll}
      >
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
                stats={statsById.get(creator.id)}
              />
            ))}
          </div>
        ) : null}

        {!isLoading && !error && displayed.length > 0 && view === 'table' ? (
          <Table columns={columns}>
            <TableBody>
              {displayed.map((creator) => (
                <CreatorRow key={creator.id} creator={creator} stats={statsById.get(creator.id)} />
              ))}
            </TableBody>
          </Table>
        ) : null}
      </PageContentBody>
    </PageContent>
  );
}

export default CreatorIndex;
