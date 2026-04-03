import { useMutation, useQueryClient } from '@tanstack/react-query';
import useApiMutation from 'Helpers/Hooks/useApiMutation';
import useApiQuery from 'Helpers/Hooks/useApiQuery';
import Field from 'typings/Field';
import { ApiError } from 'Utilities/Fetch/fetchJson';
import getQueryPath from 'Utilities/Fetch/getQueryPath';

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

  return useApiMutation<number, MetadataSourceResource>({
    path: `${PATH}/${id}`,
    method: 'PUT',
    mutationOptions: {
      onSuccess: async () => {
        // PUT returns 202 with just the ID, not the full resource.
        // Invalidate so the list is refetched with fresh field values.
        await queryClient.invalidateQueries({ queryKey: [PATH] });
      },
    },
  });
};

export const useMetadataSourceSchemas = () =>
  useApiQuery<MetadataSourceResource[]>({ path: `${PATH}/schema` });

export const useCreateMetadataSource = () => {
  const queryClient = useQueryClient();

  return useApiMutation<MetadataSourceResource, MetadataSourceResource>({
    path: PATH,
    method: 'POST',
    mutationOptions: {
      onSuccess: async () => {
        await queryClient.invalidateQueries({ queryKey: [PATH] });
      },
    },
  });
};

export const useTestMetadataSource = () =>
  useApiMutation<void, MetadataSourceResource>({
    path: `${PATH}/test`,
    method: 'POST',
  });

export const useDeleteMetadataSource = (id: number) => {
  const queryClient = useQueryClient();

  return useApiMutation<void, void>({
    path: `${PATH}/${id}`,
    method: 'DELETE',
    mutationOptions: {
      onSuccess: async () => {
        await queryClient.invalidateQueries({ queryKey: [PATH] });
      },
    },
  });
};

export interface CookieStatusResource {
  hasCookies: boolean;
}

export const useCookieStatus = (id: number) =>
  useApiQuery<CookieStatusResource>({ path: `${PATH}/${id}/cookies` });

export const useUploadCookies = (id: number) => {
  const queryClient = useQueryClient();

  return useMutation<CookieStatusResource, ApiError, File>({
    mutationFn: async (file: File) => {
      const formData = new FormData();
      formData.append('file', file);

      const response = await fetch(getQueryPath(`${PATH}/${id}/cookies`), {
        method: 'POST',
        headers: {
          'X-Api-Key': window.Streamarr.apiKey,
          'X-Streamarr-Client': 'Streamarr',
        },
        body: formData,
      });

      if (!response.ok) {
        throw new ApiError(
          `${PATH}/${id}/cookies`,
          response.status,
          response.statusText
        );
      }

      return response.json() as Promise<CookieStatusResource>;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: [`${PATH}/${id}/cookies`],
      });
    },
  });
};

export const useDeleteCookies = (id: number) => {
  const queryClient = useQueryClient();

  return useApiMutation<CookieStatusResource, void>({
    path: `${PATH}/${id}/cookies`,
    method: 'DELETE',
    mutationOptions: {
      onSuccess: async () => {
        await queryClient.invalidateQueries({
          queryKey: [`${PATH}/${id}/cookies`],
        });
      },
    },
  });
};

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
