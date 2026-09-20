import React from 'react';

interface MatchLogsProps {
  logs: string[];
}

export const MatchLogs: React.FC<MatchLogsProps> = ({ logs }) => {
  return (
    <div className="match-logs-box glass-panel">
      {logs.slice(-15).reverse().map((log, index) => (
        <div key={index} className="log-entry">
          {log}
        </div>
      ))}
    </div>
  );
};
