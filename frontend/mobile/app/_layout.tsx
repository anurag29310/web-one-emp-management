import React, { useEffect } from 'react'
import { SplashScreen, Stack } from 'expo-router'
import { GestureHandlerRootView } from 'react-native-gesture-handler'
import { SafeAreaProvider } from 'react-native-safe-area-context'
import { AuthProvider, useAuth } from '@/context/AuthContext'

// Prevent the splash screen from auto hiding
SplashScreen.preventAutoHideAsync()

function RootLayoutNav() {
  const { isAuthenticated, isInitializing } = useAuth()

  useEffect(() => {
    if (!isInitializing) {
      SplashScreen.hideAsync()
    }
  }, [isInitializing])

  if (isInitializing) {
    return null
  }

  return (
    <Stack screenOptions={{ headerShown: false, animationEnabled: false }}>
      {isAuthenticated ? (
        <Stack.Screen name="(app)" />
      ) : (
        <Stack.Screen name="(auth)" />
      )}
    </Stack>
  )
}

export default function RootLayout() {
  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <AuthProvider>
          <RootLayoutNav />
        </AuthProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  )
}
