const ONES = [
  '',
  'One',
  'Two',
  'Three',
  'Four',
  'Five',
  'Six',
  'Seven',
  'Eight',
  'Nine',
  'Ten',
  'Eleven',
  'Twelve',
  'Thirteen',
  'Fourteen',
  'Fifteen',
  'Sixteen',
  'Seventeen',
  'Eighteen',
  'Nineteen',
]

const TENS = ['', '', 'Twenty', 'Thirty', 'Forty', 'Fifty', 'Sixty', 'Seventy', 'Eighty', 'Ninety']

function underThousand(value: number): string {
  if (value === 0) return ''

  if (value < 20) return ONES[value]

  if (value < 100) {
    const tens = TENS[Math.floor(value / 10)]
    const ones = ONES[value % 10]
    return ones ? `${tens} ${ones}` : tens
  }

  const hundreds = `${ONES[Math.floor(value / 100)]} Hundred`
  const rest = underThousand(value % 100)
  return rest ? `${hundreds} ${rest}` : hundreds
}

/**
 * Spells a rupee amount the way an Indian tax invoice is expected to — lakh and crore rather than
 * million and billion. A bill has to carry the total in words as well as figures, and the two
 * numbering systems diverge above 99,999, so `Intl` cannot do this.
 */
export function amountInWords(amount: number): string {
  if (!Number.isFinite(amount)) return ''

  const rounded = Math.round(Math.abs(amount) * 100) / 100
  const rupees = Math.floor(rounded)
  const paise = Math.round((rounded - rupees) * 100)

  const groups: string[] = []
  const crore = Math.floor(rupees / 10_000_000)
  const lakh = Math.floor((rupees % 10_000_000) / 100_000)
  const thousand = Math.floor((rupees % 100_000) / 1_000)
  const remainder = rupees % 1_000

  if (crore) groups.push(`${underThousand(crore)} Crore`)
  if (lakh) groups.push(`${underThousand(lakh)} Lakh`)
  if (thousand) groups.push(`${underThousand(thousand)} Thousand`)
  if (remainder) groups.push(underThousand(remainder))

  const rupeeWords = groups.length ? groups.join(' ') : 'Zero'
  const sign = amount < 0 ? 'Minus ' : ''

  if (paise) {
    return `${sign}Rupees ${rupeeWords} and ${underThousand(paise)} Paise Only`
  }

  return `${sign}Rupees ${rupeeWords} Only`
}
