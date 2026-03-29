import React, { useCallback, useState } from 'react';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import SpinnerButton from 'Components/Link/SpinnerButton';
import ModalBody from 'Components/Modal/ModalBody';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroupBase from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import { inputTypes, kinds } from 'Helpers/Props';
import type { InputType } from 'Helpers/Props/inputTypes';
import { InputChanged } from 'typings/inputs';

// Cast to avoid TypeScript union exhaustion on the generic `type` prop.
// Each concrete usage in custom forms passes a specific inputTypes constant and
// gets full type narrowing; here we accept any input type at runtime.
const FormInputGroup = FormInputGroupBase as React.FC<{
  type: InputType;
  name: string;
  value: unknown;
  helpText?: string;
  helpTextWarning?: string;
  placeholder?: string;
  errors: unknown[];
  warnings: unknown[];
  onChange: (change: InputChanged) => void;
}>;
import {
  MetadataSourceResource,
  applyFieldChanges,
  useCreateMetadataSource,
  useDeleteMetadataSource,
  useTestMetadataSource,
  useUpdateMetadataSource,
} from 'Settings/Sources/useMetadataSources';
import { SourceFormProps } from './types';

// Maps backend FieldType serialized strings to frontend inputTypes.
const FIELD_TYPE_MAP: Record<string, InputType> = {
  textbox: inputTypes.TEXT,
  number: inputTypes.NUMBER,
  password: inputTypes.PASSWORD,
  checkbox: inputTypes.CHECK,
  select: inputTypes.SELECT,
  path: inputTypes.PATH,
  filePath: inputTypes.PATH,
  tag: inputTypes.TAG,
  url: inputTypes.TEXT,
};

function resolveInputType(fieldType: string): InputType | null {
  if (fieldType === 'action') {
    return null;
  }
  return FIELD_TYPE_MAP[fieldType] ?? inputTypes.TEXT;
}

function GenericSourceForm({ source, onModalClose }: SourceFormProps) {
  const isNew = !source.id;
  const title = source.implementationName || source.implementation;

  const [enabled, setEnabled] = useState(source.enable ?? true);
  const [pending, setPending] = useState<Record<string, unknown>>({});
  const [testResult, setTestResult] = useState<'success' | 'failure' | null>(null);
  const [testMessage, setTestMessage] = useState('');

  const { mutate: create, isPending: isCreating } = useCreateMetadataSource();
  const { mutate: update, isPending: isUpdating } = useUpdateMetadataSource(source.id ?? 0);
  const isSaving = isCreating || isUpdating;
  const { mutate: deleteSource, isPending: isDeleting } = useDeleteMetadataSource(source.id ?? 0);
  const { mutate: runTest, isPending: isTesting } = useTestMetadataSource();

  const getVal = useCallback(
    (name: string) => {
      if (name in pending) return pending[name];
      const field = source.fields.find((f) => f.name === name);
      return field?.value;
    },
    [pending, source.fields]
  );

  const handleInputChange = useCallback((change: InputChanged) => {
    setPending((prev) => ({ ...prev, [change.name]: change.value }));
  }, []);

  const buildUpdatedSource = useCallback(
    (): MetadataSourceResource => ({
      ...source,
      enable: enabled,
      fields: applyFieldChanges(source.fields, pending),
    }),
    [source, enabled, pending]
  );

  const handleTest = useCallback(() => {
    runTest(buildUpdatedSource(), {
      onSuccess: () => {
        setTestResult('success');
        setTestMessage('Connection successful');
      },
      onError: (err) => {
        setTestResult('failure');
        setTestMessage(err.statusBody?.message ?? err.statusText ?? 'Connection failed');
      },
    });
  }, [runTest, buildUpdatedSource]);

  const handleSave = useCallback(() => {
    const updated = buildUpdatedSource();
    const save = isNew ? create : update;
    save(updated, {
      onSuccess: () => onModalClose(),
      onError: (err) => {
        setTestResult('failure');
        setTestMessage(err.statusBody?.message ?? err.statusText ?? 'Save failed');
      },
    });
  }, [buildUpdatedSource, isNew, create, update, onModalClose]);

  const visibleFields = source.fields
    .filter((f) => {
      if (f.hidden === 'hidden') return false;
      if (f.hidden === 'hiddenIfNotSet') {
        const val = getVal(f.name);
        return val !== null && val !== undefined && val !== '';
      }
      return true;
    })
    .sort((a, b) => a.order - b.order);

  return (
    <>
      <ModalHeader>{title}</ModalHeader>

      <ModalBody>
        <FormGroup>
          <FormLabel>Enable</FormLabel>
          <FormInputGroup
            type={inputTypes.CHECK}
            name="enable"
            helpText="Enable this source for content syncing and channel searches."
            value={enabled}
            errors={[]}
            warnings={[]}
            onChange={(change: InputChanged) => setEnabled(change.value as boolean)}
          />
        </FormGroup>

        {visibleFields.map((field) => {
          const inputType = resolveInputType(field.type);
          if (inputType === null) return null;

          return (
            <FormGroup key={field.name}>
              <FormLabel>{field.label}</FormLabel>
              <FormInputGroup
                type={inputType}
                name={field.name}
                helpText={field.helpText}
                helpTextWarning={field.helpTextWarning}
                placeholder={field.placeholder}
                value={getVal(field.name) ?? (inputType === inputTypes.CHECK ? false : '')}
                errors={[]}
                warnings={[]}
                onChange={handleInputChange}
              />
            </FormGroup>
          );
        })}

        {testResult !== null && (
          <Alert kind={testResult === 'success' ? 'success' : 'danger'}>
            {testMessage}
          </Alert>
        )}
      </ModalBody>

      <ModalFooter>
        {!isNew && (
          <div style={{ marginRight: 'auto' }}>
            <SpinnerButton
              kind={kinds.DANGER}
              isSpinning={isDeleting}
              onPress={() => deleteSource(undefined, { onSuccess: () => onModalClose() })}
            >
              Delete
            </SpinnerButton>
          </div>
        )}

        <Button onPress={onModalClose}>Cancel</Button>

        <SpinnerButton isSpinning={isTesting} onPress={handleTest}>
          Test
        </SpinnerButton>

        <SpinnerButton isSpinning={isSaving} onPress={handleSave}>
          {isNew ? 'Add' : 'Save'}
        </SpinnerButton>
      </ModalFooter>
    </>
  );
}

export default GenericSourceForm;
