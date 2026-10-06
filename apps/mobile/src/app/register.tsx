import { Link, router } from "expo-router";
import { useState } from "react";
import { Pressable, StyleSheet, Text, TextInput, View } from "react-native";
import { ApiError, api, type AuthResponse } from "../api";
import { useSession } from "../session";

export default function RegisterScreen() {
  const { signIn } = useSession();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [pending, setPending] = useState(false);

  async function submit() {
    setPending(true);
    setError("");
    try {
      const auth = await api<AuthResponse>("/auth/register", {
        method: "POST",
        body: { email, password },
      });
      await signIn(auth.accessToken);
      router.replace("/home");
    } catch (caught) {
      setError(messageFor(caught));
    } finally {
      setPending(false);
    }
  }

  return (
    <View style={styles.screen}>
      <Text style={styles.title}>Criar conta</Text>
      <Text style={styles.subtitle}>A carteira começa com R$ 50,00</Text>
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
        <Text style={styles.buttonText}>{pending ? "Criando..." : "Cadastrar"}</Text>
      </Pressable>
      <Link href="/login" style={styles.link}>
        Já tenho conta
      </Link>
    </View>
  );
}

function messageFor(caught: unknown) {
  if (!(caught instanceof ApiError)) {
    return "Não foi possível criar a conta.";
  }
  if (caught.code === "email_taken") {
    return "Esse e-mail já está cadastrado.";
  }
  if (caught.code === "invalid_request") {
    return "Use um e-mail válido e uma senha de 8 a 128 caracteres.";
  }
  if (caught.code === "network") {
    return "Não foi possível falar com a API.";
  }
  return "Não foi possível criar a conta.";
}

const styles = StyleSheet.create({
  screen: { flex: 1, justifyContent: "center", padding: 24, backgroundColor: "#f4f7f7" },
  title: { fontSize: 28, fontWeight: "700", color: "#0f3d3e" },
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
