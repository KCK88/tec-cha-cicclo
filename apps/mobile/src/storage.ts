import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

const accessKey = "cicclo.accessToken";
const refreshKey = "cicclo.refreshToken";

export async function readAccessToken() {
  if (Platform.OS === "web") {
    return localStorage.getItem(accessKey);
  }
  return SecureStore.getItemAsync(accessKey);
}

export async function writeAccessToken(token: string | null) {
  if (Platform.OS === "web") {
    if (token) {
      localStorage.setItem(accessKey, token);
    } else {
      localStorage.removeItem(accessKey);
    }
    return;
  }

  if (token) {
    await SecureStore.setItemAsync(accessKey, token);
  } else {
    await SecureStore.deleteItemAsync(accessKey);
  }
}

export async function readRefreshToken() {
  if (Platform.OS === "web") {
    return localStorage.getItem(refreshKey);
  }
  return SecureStore.getItemAsync(refreshKey);
}

export async function writeRefreshToken(token: string | null) {
  if (Platform.OS === "web") {
    if (token) {
      localStorage.setItem(refreshKey, token);
    } else {
      localStorage.removeItem(refreshKey);
    }
    return;
  }

  if (token) {
    await SecureStore.setItemAsync(refreshKey, token);
  } else {
    await SecureStore.deleteItemAsync(refreshKey);
  }
}
