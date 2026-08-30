import { createContext, useContext, useMemo, useState } from 'react';

const STORAGE_KEY = 'storygen-ai-provider';
const DEFAULT_PROVIDER = 'gemini';
const VALID_PROVIDERS = ['gemini', 'cohere', 'claude'];

const AIProviderContext = createContext(null);

export function AIProviderProvider({ children }) {
  const [provider, setProvider] = useState(() => {
    const saved = window.localStorage.getItem(STORAGE_KEY);
    return VALID_PROVIDERS.includes(saved) ? saved : DEFAULT_PROVIDER;
  });

  const value = useMemo(() => ({
    provider,
    setProvider: (nextProvider) => {
      const normalized = VALID_PROVIDERS.includes(nextProvider) ? nextProvider : DEFAULT_PROVIDER;
      window.localStorage.setItem(STORAGE_KEY, normalized);
      setProvider(normalized);
    },
    providerLabel: {
      gemini: 'Gemini',
      cohere: 'Cohere',
      claude: 'Claude'
    }[provider] || 'Gemini',
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
