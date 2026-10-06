import { Redirect } from "expo-router";
import { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from "react-native";
import { ApiError, api, type LaundryService, type PurchaseResponse, type WalletResponse } from "../api";
import { formatBrl } from "../money";
import { useSession } from "../session";

export default function HomeScreen() {
  const { token, ready, signOut } = useSession();
  const [balanceCents, setBalanceCents] = useState<number | null>(null);
  const [services, setServices] = useState<LaundryService[]>([]);
  const [message, setMessage] = useState("");
  const [messageIsError, setMessageIsError] = useState(false);
  const [pendingId, setPendingId] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!token) {
      return;
    }
    const [wallet, listed] = await Promise.all([
      api<WalletResponse>("/wallet", { token }),
      api<LaundryService[]>("/services", { token }),
    ]);
    setBalanceCents(wallet.balanceCents);
    setServices(listed);
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

  return (
    <View style={styles.screen}>
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
          <Pressable
            style={styles.button}
            disabled={pendingId !== null}
            onPress={() => purchase(service)}
          >
            <Text style={styles.buttonText}>{pendingId === service.id ? "..." : "Solicitar"}</Text>
          </Pressable>
        </View>
      ))}
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, padding: 24, paddingTop: 64, backgroundColor: "#f4f7f7" },
  centered: { flex: 1, alignItems: "center", justifyContent: "center" },
  header: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  title: { fontSize: 28, fontWeight: "700", color: "#0f3d3e" },
  label: { marginTop: 24, color: "#3d5c5c" },
  balance: { fontSize: 36, fontWeight: "700", color: "#0f3d3e", marginBottom: 16 },
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
  error: { color: "#9b2c2c", marginBottom: 12 },
  success: { color: "#0f6e6e", marginBottom: 12 },
  link: { color: "#0f6e6e" },
});
