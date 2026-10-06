import { Link, router } from "expo-router";
import { useState } from "react";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import { ApiError, api, type AuthResponse } from "../api";
import { useSession } from "../session";

export default function LoginScreen() {
  const { signIn } = useSession();
  const [email, setEmail] = useState("usuario@cicclo.dev");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  async function submit() {
    setPending(true);
    setError("");
    try {
      const auth = await api<AuthResponse>("/auth/login", {
        method: "POST",
        body: { email, password },
      });
      await signIn(auth.accessToken, auth.refreshToken);
      router.replace("/home");
    } catch (caught) {
      setError(messageFor(caught));
    } finally {
      setPending(false);
    }
  }

  return (
    <View style={styles.screen}>
      <Text style={styles.title}>Cicclo</Text>
      <Text style={styles.subtitle}>Entre para usar a lavanderia</Text>
      <TextInput
        autoCapitalize="none"
        keyboardType="email-address"
        placeholder="E-mail"
        style={styles.input}
        value={email}
        onChangeText={setEmail}
      />
      <TextInput
        placeholder="Senha"
        secureTextEntry
        style={styles.input}
        value={password}
        onChangeText={setPassword}
      />
      {error ? <Text style={styles.error}>{error}</Text> : null}
      <Pressable style={styles.button} disabled={pending} onPress={submit}>
        <Text style={styles.buttonText}>{pending ? "Entrando..." : "Entrar"}</Text>
      </Pressable>
      <Link href="/register" style={styles.link}>
        Criar conta
      </Link>
    </View>
  );
}

function messageFor(caught: unknown) {
  if (caught instanceof ApiError && caught.code === "invalid_credentials") {
    return "E-mail ou senha incorretos.";
  }
  if (caught instanceof ApiError && caught.code === "network") {
    return "Não foi possível falar com a API.";
  }
  return "Não foi possível entrar.";
}

const styles = StyleSheet.create({
  screen: { flex: 1, justifyContent: "center", padding: 24, backgroundColor: "#f4f7f7" },
  title: { fontSize: 32, fontWeight: "700", color: "#0f3d3e" },
  subtitle: { marginTop: 8, marginBottom: 24, color: "#3d5c5c" },
  input: {
    backgroundColor: "#fff",
    borderWidth: 1,
    borderColor: "#d5e2e2",
    borderRadius: 8,
    padding: 12,
    marginBottom: 12,
  },
  button: { backgroundColor: "#0f6e6e", borderRadius: 8, padding: 14, alignItems: "center" },
  buttonText: { color: "#fff", fontWeight: "700" },
  error: { color: "#9b2c2c", marginBottom: 12 },
  link: { marginTop: 16, color: "#0f6e6e", textAlign: "center" },
});
