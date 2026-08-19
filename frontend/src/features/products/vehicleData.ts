/**
 * Vehicle brand → model options for the Product master.
 *
 * Kept in the frontend for now: the shop only needs a fixed, well-known list and the MVP spec
 * explicitly avoids extra master tables. Promoting this to a DB-backed master later only means
 * swapping the two `useMemo`s in ProductFormFields for a query hook — the field names don't change.
 */
export const VEHICLE_BRANDS = [
  'Honda',
  'TVS',
  'Bajaj',
  'Hero',
  'Yamaha',
  'Suzuki',
  'Royal Enfield',
  'KTM',
  'Other',
] as const

export const VEHICLE_MODELS: Record<string, string[]> = {
  Honda: [
    'Activa 6G',
    'Activa 125',
    'Dio',
    'Shine',
    'SP 125',
    'Unicorn',
    'Hornet 2.0',
    'Livo',
  ],
  TVS: [
    'Jupiter',
    'Ntorq 125',
    'Apache RTR 160',
    'Apache RTR 200',
    'Raider 125',
    'Sport',
    'Star City Plus',
    'XL 100',
  ],
  Bajaj: [
    'Pulsar 125',
    'Pulsar 150',
    'Pulsar NS200',
    'Platina 100',
    'CT 100',
    'Avenger 220',
    'Dominar 400',
    'Chetak',
  ],
  Hero: [
    'Splendor Plus',
    'HF Deluxe',
    'Passion Pro',
    'Glamour',
    'Xtreme 160R',
    'Pleasure Plus',
    'Maestro Edge 125',
    'Destini 125',
  ],
  Yamaha: ['FZ-S FI', 'FZ-X', 'R15 V4', 'MT-15', 'Fascino 125', 'Ray ZR 125'],
  Suzuki: ['Access 125', 'Burgman Street', 'Avenis 125', 'Gixxer', 'Gixxer SF'],
  'Royal Enfield': [
    'Classic 350',
    'Bullet 350',
    'Hunter 350',
    'Meteor 350',
    'Himalayan',
    'Interceptor 650',
  ],
  KTM: ['Duke 200', 'Duke 250', 'Duke 390', 'RC 200', 'RC 390'],
  Other: [],
}

/** GST unit quantity codes relevant to a spare-parts counter. */
export const UQC_OPTIONS = [
  { value: 'PCS', label: 'PCS — Pieces' },
  { value: 'NOS', label: 'NOS — Numbers' },
  { value: 'SET', label: 'SET — Set' },
  { value: 'PRS', label: 'PRS — Pairs' },
  { value: 'BOX', label: 'BOX — Box' },
  { value: 'DOZ', label: 'DOZ — Dozen' },
  { value: 'KGS', label: 'KGS — Kilograms' },
  { value: 'LTR', label: 'LTR — Litres' },
  { value: 'MTR', label: 'MTR — Metres' },
]
