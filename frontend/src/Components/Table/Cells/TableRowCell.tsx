import classNames from 'classnames';
import React, { ComponentPropsWithoutRef } from 'react';
import styles from './TableRowCell.css';

export type TableRowCellProps = ComponentPropsWithoutRef<'td'>;

export default function TableRowCell({
  className,
  ...tdProps
}: TableRowCellProps) {
  return <td className={classNames(styles.cell, className)} {...tdProps} />;
}
