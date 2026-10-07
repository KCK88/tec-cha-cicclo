import { Redirect } from "expo-router";
import { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from "react-native";
import {
  ApiError,
  api,
  type LaundryService,
  type PurchaseResponse,
  type WalletEntry,
  type WalletResponse,
} from "../api";
import { formatBrl, maskReaisInput, reaisToCents } from "../money";
import { useSession } from "../session";

export default function HomeScreen() {
  const { token, ready, signOut } = useSession();
  const [balanceCents, setBalanceCents] = useState<number | null>(null);
  const [services, setServices] = useState<LaundryService[]>([]);
  const [entries, setEntries] = useState<WalletEntry[]>([]);
  const [topUp, setTopUp] = useState("");
  const [message, setMessage] = useState("");
  const [messageIsError, setMessageIsError] = useState(false);
  const [pendingId, setPendingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!token) {
      return;
    }
    const [wallet, listed, statement] = await Promise.all([
      api<WalletResponse>("/wallet", { token }),
      api<LaundryService[]>("/services", { token }),
      api<WalletEntry[]>("/wallet/entries", { token }),
    ]);
    setBalanceCents(wallet.balanceCents);
    setServices(listed);
    setEntries(statement);
  }, [token]);

  useEffect(() => {
    if (token) {
      load().catch(() => {
        setMessageIsError(true);
        setMessage("Não foi possível carregar a carteira.");
      });
    }
  }, [token, load]);

  if (!ready) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator />
      </View>
    );
  }
  if (!token) {
    return <Redirect href="/login" />;
  }

  async function purchase(service: LaundryService) {
    setPendingId(service.id);
    setMessage("");
    try {
      const result = await api<PurchaseResponse>(`/services/${service.id}/purchases`, {
        method: "POST",
        token,
      });
      setBalanceCents(result.balanceCents);
      setMessageIsError(false);
      setMessage(`${result.serviceName} solicitada. Saldo atualizado.`);
      await load();
    } catch (caught) {
      setMessageIsError(true);
      if (caught instanceof ApiError && caught.code === "insufficient_balance") {
        setMessage(`Saldo insuficiente para ${service.name}. O saldo continua ${formatBrl(caught.balanceCents ?? balanceCents ?? 0)}.`);
      } else {
        setMessage("Não foi possível solicitar o serviço.");
      }
    } finally {
      setPendingId(null);
    }
  }

  async function addCredit() {
    const amountCents = reaisToCents(topUp);
    if (amountCents === null) {
      setMessageIsError(true);
      setMessage("Informe um valor maior que zero.");
      return;
    }
    setPendingId("top-up");
    setMessage("");
    try {
      const result = await api<WalletResponse>("/wallet/top-ups", {
        method: "POST",
        token,
        body: { amountCents },
      });
      setBalanceCents(result.balanceCents);
      setTopUp("");
      setMessageIsError(false);
      setMessage("Recarga feita.");
      await load();
    } catch {
      setMessageIsError(true);
      setMessage("Não foi possível recarregar.");
    } finally {
      setPendingId(null);
    }
  }

  async function cancel(entry: WalletEntry) {
    setPendingId(entry.id);
    setMessage("");
    try {
      const result = await api<WalletResponse>(`/wallet/entries/${entry.id}/cancellations`, {
        method: "POST",
        token,
      });
      setBalanceCents(result.balanceCents);
      setMessageIsError(false);
      setMessage("Compra cancelada. O valor voltou para o saldo.");
      await load();
    } catch (caught) {
      setMessageIsError(true);
      if (caught instanceof ApiError && caught.code === "already_cancelled") {
        setMessage("Essa compra já foi cancelada.");
      } else {
        setMessage("Não foi possível cancelar.");
      }
    } finally {
      setPendingId(null);
    }
  }

  return (
    <ScrollView contentContainerStyle={styles.screen}>
      <View style={styles.header}>
        <Text style={styles.title}>Cicclo</Text>
        <Pressable onPress={() => signOut()}>
          <Text style={styles.link}>Sair</Text>
        </Pressable>
      </View>
      <Text style={styles.label}>Saldo</Text>
      <Text style={styles.balance}>{balanceCents === null ? "..." : formatBrl(balanceCents)}</Text>
      {message ? <Text style={messageIsError ? styles.error : styles.success}>{message}</Text> : null}
      {services.map((service) => (
        <View key={service.id} style={styles.card}>
          <View>
            <Text style={styles.serviceName}>{service.name}</Text>
            <Text style={styles.price}>{formatBrl(service.priceCents)}</Text>
          </View>
          <Pressable style={styles.button} disabled={pendingId !== null} onPress={() => purchase(service)}>
            <Text style={styles.buttonText}>{pendingId === service.id ? "..." : "Solicitar"}</Text>
          </Pressable>
        </View>
      ))}
      <Text style={styles.section}>Recarga</Text>
      <View style={styles.row}>
        <TextInput
          keyboardType="number-pad"
          inputMode="numeric"
          placeholder="0,00"
          style={styles.input}
          value={topUp}
          onChangeText={(value) => setTopUp(maskReaisInput(value))}
        />
        <Pressable style={styles.button} disabled={pendingId !== null} onPress={addCredit}>
          <Text style={styles.buttonText}>{pendingId === "top-up" ? "..." : "Recarregar"}</Text>
        </Pressable>
      </View>
      <Text style={styles.section}>Extrato</Text>
      {entries.length === 0 ? <Text style={styles.price}>Nenhum lançamento ainda.</Text> : null}
      {entries.map((entry) => (
        <View key={entry.id} style={styles.card}>
          <View>
            <Text style={styles.serviceName}>{entryLabel(entry)}</Text>
            <Text style={styles.price}>{signedAmount(entry)}</Text>
          </View>
          {entry.kind === "purchase" && !entry.cancelled ? (
            <Pressable style={styles.secondary} disabled={pendingId !== null} onPress={() => cancel(entry)}>
              <Text style={styles.secondaryText}>{pendingId === entry.id ? "..." : "Cancelar"}</Text>
            </Pressable>
          ) : null}
        </View>
      ))}
    </ScrollView>
  );
}

function entryLabel(entry: WalletEntry) {
  if (entry.kind === "top_up") {
    return "Recarga";
  }
  if (entry.kind === "cancellation") {
    return `Cancelamento${entry.serviceName ? ` de ${entry.serviceName}` : ""}`;
  }
  return entry.cancelled ? `${entry.serviceName ?? "Compra"} cancelada` : (entry.serviceName ?? "Compra");
}

function signedAmount(entry: WalletEntry) {
  const formatted = formatBrl(entry.amountCents);
  return entry.kind === "purchase" && !entry.cancelled ? `-${formatted}` : `+${formatted}`;
}

const styles = StyleSheet.create({
  screen: { padding: 24, paddingTop: 64, paddingBottom: 48, backgroundColor: "#f4f7f7" },
  centered: { flex: 1, alignItems: "center", justifyContent: "center" },
  header: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  title: { fontSize: 28, fontWeight: "700", color: "#0f3d3e" },
  label: { marginTop: 24, color: "#3d5c5c" },
  balance: { fontSize: 36, fontWeight: "700", color: "#0f3d3e", marginBottom: 16 },
  section: { marginTop: 20, marginBottom: 8, fontSize: 18, fontWeight: "700", color: "#0f3d3e" },
  card: {
    backgroundColor: "#fff",
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
  },
  serviceName: { fontSize: 18, fontWeight: "600", color: "#0f3d3e" },
  price: { color: "#3d5c5c", marginTop: 4 },
  button: { backgroundColor: "#0f6e6e", borderRadius: 8, paddingVertical: 10, paddingHorizontal: 14 },
  buttonText: { color: "#fff", fontWeight: "700" },
  secondary: { borderWidth: 1, borderColor: "#0f6e6e", borderRadius: 8, paddingVertical: 10, paddingHorizontal: 14 },
  secondaryText: { color: "#0f6e6e", fontWeight: "700" },
  error: { color: "#9b2c2c", marginBottom: 12 },
  success: { color: "#0f6e6e", marginBottom: 12 },
  link: { color: "#0f6e6e" },
  row: { flexDirection: "row", alignItems: "center", gap: 12 },
  input: {
    flex: 1,
    backgroundColor: "#fff",
    borderWidth: 1,
    borderColor: "#d5e2e2",
    borderRadius: 8,
    padding: 12,
  },
});
