import type { ComponentType } from 'react'
import { ClassicTemplate } from './ClassicTemplate'
import { DetailedTemplate } from './DetailedTemplate'
import { MinimalTemplate } from './MinimalTemplate'
import { ModernTemplate } from './ModernTemplate'
import { TraditionalTemplate } from './TraditionalTemplate'
import type { InvoiceTemplateId, InvoiceTemplateProps } from './types'

export type InvoiceTemplateOption = {
  id: InvoiceTemplateId
  name: string
  /** One line, written for the shopkeeper choosing — what it is for, not what it looks like. */
  description: string
  paper: string
  component: ComponentType<InvoiceTemplateProps>
}

/**
 * Every template the app can print. Order is the order they appear in Settings, so the default
 * comes first and the more specialised layouts follow.
 */
export const INVOICE_TEMPLATES: InvoiceTemplateOption[] = [
  {
    id: 'Classic',
    name: 'Classic',
    description: 'Clean and readable, with the tax split as columns. A good default.',
    paper: 'A4',
    component: ClassicTemplate,
  },
  {
    id: 'Detailed',
    name: 'Detailed',
    description: 'Adds a rate-wise tax summary, bank details and terms — what your accountant wants.',
    paper: 'A4',
    component: DetailedTemplate,
  },
  {
    id: 'Modern',
    name: 'Modern',
    description: 'A colour band and large type. For a bill you want to look designed.',
    paper: 'A4',
    component: ModernTemplate,
  },
  {
    id: 'Traditional',
    name: 'Traditional',
    description: 'Fully ruled boxes, like a printed invoice book. Survives a photocopy.',
    paper: 'A4',
    component: TraditionalTemplate,
  },
  {
    id: 'Minimal',
    name: 'Minimal',
    description: 'Hairlines and white space only. Nothing on the page that is not needed.',
    paper: 'A4',
    component: MinimalTemplate,
  },
]

/**
 * The chosen template's component, falling back to Classic. A settings row written by a newer
 * version of the app must not leave the counter unable to print.
 */
export function templateComponent(id: string | undefined): ComponentType<InvoiceTemplateProps> {
  return INVOICE_TEMPLATES.find((t) => t.id === id)?.component ?? ClassicTemplate
}

export type { InvoiceTemplateId, InvoiceTemplateProps }
