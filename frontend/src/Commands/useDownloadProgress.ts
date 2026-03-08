import { useMemo } from 'react';
import { useCommands } from './useCommands';

const PERCENT_RE = /(\d+(?:\.\d+)?)%/;

/**
 * Returns a map of contentId → download percent (0–100) for all
 * DownloadContent commands that are currently in progress.
 */
export function useDownloadProgress(): Map<number, number> {
  const { data: commands } = useCommands();

  return useMemo(() => {
    const map = new Map<number, number>();

    for (const command of commands) {
      if (command.name !== 'DownloadContent') continue;
      if (command.status !== 'started') continue;

      const contentId = command.body?.contentId;
      if (contentId == null) continue;

      const match = command.message ? PERCENT_RE.exec(command.message) : null;
      if (match) {
        map.set(contentId, parseFloat(match[1]));
      }
    }

    return map;
  }, [commands]);
}

export default useDownloadProgress;
