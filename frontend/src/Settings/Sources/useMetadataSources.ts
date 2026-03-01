import { useQueryClient } from '@tanstack/react-query';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import useApiQuery from 'Helpers/Hooks/useApiQuery';
import Field from 'typings/Field';

export interface MetadataSourceResource {
  id: number;
  name: string;
  enable: boolean;
  platform: number;
  fields: Field[];
  implementationName: string;
  implementation: string;
  configContract: string;
}

const PATH = '/metadatasource';

export const useMetadataSources = () =>
  useApiQuery<MetadataSourceResource[]>({ path: PATH });

export const useUpdateMetadataSource = (id: number) => {
  const queryClient = useQueryClient();

  return useApiMutation<MetadataSourceResource, MetadataSourceResource>({
    path: `${PATH}/${id}`,
    method: 'PUT',
    mutationOptions: {
      onSuccess: (updated) => {
        queryClient.setQueryData<MetadataSourceResource[]>(
          [PATH],
          (old = []) => old.map((s) => (s.id === updated.id ? updated : s))
        );
      },
    },
  });
};

export const useTestMetadataSource = () =>
  useApiMutation<void, MetadataSourceResource>({
    path: `${PATH}/test`,
    method: 'POST',
  });

export function getFieldValue<T>(fields: Field[], name: string, fallback: T): T {
  const field = fields.find((f) => f.name === name);

  return field !== undefined ? (field.value as T) : fallback;
}

export function applyFieldChanges(
  fields: Field[],
  changes: Record<string, unknown>
): Field[] {
  return fields.map((f) =>
    f.name in changes ? { ...f, value: changes[f.name] as Field['value'] } : f
  );
}
