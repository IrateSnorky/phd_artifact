import { createContext, useContext, useState } from 'react';

const QUOTA_MESSAGE = 'The free-tier AI quota has been reached. Please try again later, after the quota has reset.';

const QuotaContext = createContext(null);

export function QuotaProvider({ children }) {
  const [quotaMessage, setQuotaMessage] = useState(null);

  const showQuotaMessage = () => setQuotaMessage(QUOTA_MESSAGE);
  const dismissQuotaMessage = () => setQuotaMessage(null);

  return (
    <QuotaContext.Provider value={{ showQuotaMessage }}>
      {children}
      {quotaMessage && (
        <div
          role="alert"
          style={{
            position: 'fixed',
            top: 16,
            right: 16,
            zIndex: 2000,
            display: 'flex',
            alignItems: 'center',
            gap: 12,
            maxWidth: 420,
            padding: '10px 14px',
            backgroundColor: '#fff3e6',
            border: '1px solid #FF5200',
            borderRadius: 6,
            color: '#8a2f00',
            fontSize: 14,
            boxShadow: '0 2px 8px rgba(0, 0, 0, 0.15)',
          }}
        >
          <span>{quotaMessage}</span>
          <button
            type="button"
            onClick={dismissQuotaMessage}
            aria-label="Dismiss quota message"
            style={{
              border: 'none',
              background: 'transparent',
              color: '#8a2f00',
              cursor: 'pointer',
              fontSize: 18,
              lineHeight: 1,
              padding: 0,
            }}
          >
            ×
          </button>
        </div>
      )}
    </QuotaContext.Provider>
  );
}

// oxlint-disable-next-line react/only-export-components
export function useQuotaMessage() {
  const context = useContext(QuotaContext);
  if (!context) throw new Error('useQuotaMessage must be used within QuotaProvider');
  return context;
}

// oxlint-disable-next-line react/only-export-components
export function isQuotaError(message) {
  return /quota|exceeded.*limit|resource_exhausted/i.test(message);
}
