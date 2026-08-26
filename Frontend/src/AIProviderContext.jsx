import { createContext, useContext, useMemo, useState } from 'react';

const STORAGE_KEY = 'storygen-ai-provider';
const DEFAULT_PROVIDER = 'gemini';

const AIProviderContext = createContext(null);

export function AIProviderProvider({ children }) {
  const [provider, setProvider] = useState(() => {
    const saved = window.localStorage.getItem(STORAGE_KEY);
    return saved === 'cohere' || saved === 'gemini' ? saved : DEFAULT_PROVIDER;
  });

  const value = useMemo(() => ({
    provider,
    setProvider: (nextProvider) => {
      const normalized = nextProvider === 'cohere' ? 'cohere' : DEFAULT_PROVIDER;
      window.localStorage.setItem(STORAGE_KEY, normalized);
      setProvider(normalized);
    },
    providerLabel: provider === 'cohere' ? 'Cohere' : 'Gemini',
    getHeaders: () => ({ 'X-AI-Provider': provider }),
  }), [provider]);

  return <AIProviderContext.Provider value={value}>{children}</AIProviderContext.Provider>;
}

// oxlint-disable-next-line react/only-export-components
export function useAIProvider() {
  const context = useContext(AIProviderContext);
  if (!context) throw new Error('useAIProvider must be used within AIProviderProvider');
  return context;
}
