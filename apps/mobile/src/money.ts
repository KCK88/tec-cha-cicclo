const maxTopUpCents = 1_000_000;

export function formatBrl(cents: number) {
  return (cents / 100).toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export function maskReaisInput(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 7);
  if (!digits) {
    return "";
  }
  const cents = Number(digits);
  if (!Number.isFinite(cents) || cents <= 0) {
    return "";
  }
  const capped = Math.min(Math.trunc(cents), maxTopUpCents);
  return (capped / 100).toLocaleString("pt-BR", {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
}

export function reaisToCents(value: string) {
  const digits = value.replace(/\D/g, "");
  if (!digits) {
    return null;
  }
  const cents = Number(digits);
  if (!Number.isInteger(cents) || cents <= 0 || cents > maxTopUpCents) {
    return null;
  }
  return cents;
}
