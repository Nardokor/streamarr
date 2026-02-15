import { create, type StateCreator } from 'zustand';
import { persist, type PersistOptions } from 'zustand/middleware';

export const createPersist = <T>(
  name: string,
  state: StateCreator<T>,
  options: Omit<PersistOptions<T>, 'name' | 'storage'> = {}
) => {
  const instanceName =
    window.Streamarr.instanceName.toLowerCase().replace(/ /g, '_') ?? 'streamarr';

  const finalName = `${instanceName}_${name}`;

  return create(
    persist<T>(state, {
      ...options,
      name: finalName,
    })
  );
};
