import * as SecureStore from "expo-secure-store";
import { Platform } from "react-native";

const accessKey = "cicclo.accessToken";

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
