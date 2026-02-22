import React, { useMemo } from 'react';
import Alert from 'Components/Alert';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableRow from 'Components/Table/TableRow';
import CommandNames from 'Commands/CommandNames';
import { useExecuteCommand } from 'Commands/useCommands';
import { icons, kinds } from 'Helpers/Props';
import { useAllChannels } from 'Creator/useCreators';
import useCreators from 'Creator/useCreators';
import { useWantedMissing } from './useWanted';

const columns: Column[] = [
  { name: 'title', label: 'Title', isVisible: true },
  { name: 'creator', label: 'Creator', isVisible: true },
  { name: 'channel', label: 'Channel', isVisible: true },
  { name: 'airDate', label: 'Air Date', isVisible: true },
  { name: 'duration', label: 'Duration', isVisible: true },
  { name: 'actions', label: '', isVisible: true },
];

function formatAirDate(dateStr: string | null): string {
  if (!dateStr) return '—';
  return new Date(dateStr).toLocaleDateString();
}

function formatDuration(dur: string | null): string {
  if (!dur) return '—';
  const parts = dur.split(':');
  if (parts.length === 3) {
    const h = parseInt(parts[0], 10);
    const m = parseInt(parts[1], 10);
    const s = parseInt(parts[2], 10);
    if (h > 0) return `${h}h ${m}m`;
    if (m > 0) return `${m}m ${s}s`;
    return `${s}s`;
  }
  return dur;
}

function WantedMissing() {
  const { data: missing, isLoading } = useWantedMissing();
  const { data: channels } = useAllChannels();
  const { data: creators } = useCreators();
  const executeCommand = useExecuteCommand();

  const channelMap = useMemo(() => {
    const m = new Map<number, { title: string; creatorId: number }>();
    channels.forEach((ch) => m.set(ch.id, { title: ch.title, creatorId: ch.creatorId }));
    return m;
  }, [channels]);

  const creatorMap = useMemo(() => {
    const m = new Map<number, { title: string; id: number }>();
    creators.forEach((cr) => m.set(cr.id, { title: cr.title, id: cr.id }));
    return m;
  }, [creators]);

  const sorted = useMemo(() => {
    return [...missing].sort((a, b) => {
      const da = a.airDateUtc ? new Date(a.airDateUtc).getTime() : 0;
      const db = b.airDateUtc ? new Date(b.airDateUtc).getTime() : 0;
      return db - da;
    });
  }, [missing]);

  return (
    <PageContent title="Wanted: Missing">
      <PageContentBody>
        {isLoading ? <LoadingIndicator /> : null}

        {!isLoading && sorted.length === 0 ? (
          <Alert kind={kinds.SUCCESS}>
            No missing monitored content — all caught up.
          </Alert>
        ) : null}

        {sorted.length > 0 ? (
          <Table columns={columns}>
            <TableBody>
              {sorted.map((item) => {
                const ch = channelMap.get(item.channelId);
                const creator = ch ? creatorMap.get(ch.creatorId) : undefined;

                return (
                  <TableRow key={item.id}>
                    <TableRowCell>{item.title}</TableRowCell>

                    <TableRowCell>
                      {creator ? (
                        <Link to={`/creator/${creator.id}`}>
                          {creator.title}
                        </Link>
                      ) : (
                        '—'
                      )}
                    </TableRowCell>

                    <TableRowCell>{ch?.title ?? '—'}</TableRowCell>

                    <TableRowCell>{formatAirDate(item.airDateUtc)}</TableRowCell>

                    <TableRowCell>{formatDuration(item.duration)}</TableRowCell>

                    <TableRowCell>
                      <IconButton
                        name={icons.DOWNLOAD}
                        size={12}
                        title="Download"
                        onPress={() =>
                          executeCommand({
                            name: CommandNames.DownloadContent,
                            contentId: item.id,
                          })
                        }
                      />
                    </TableRowCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        ) : null}
      </PageContentBody>
    </PageContent>
  );
}

export default WantedMissing;
