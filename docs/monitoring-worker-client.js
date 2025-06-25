/**
 * MonitoringWorker JavaScript Client Library
 * 
 * A comprehensive client library for interacting with the MonitoringWorker API
 * and managing real-time connections via SignalR.
 * 
 * @version 1.0.0
 * @author MonitoringWorker Team
 */

class MonitoringWorkerClient {
    constructor(baseUrl, options = {}) {
        this.baseUrl = baseUrl.replace(/\/$/, ''); // Remove trailing slash
        this.token = options.token || null;
        this.signalRConnection = null;
        this.eventHandlers = new Map();
        this.autoRefreshInterval = null;
        this.options = {
            autoRefresh: options.autoRefresh || false,
            refreshInterval: options.refreshInterval || 30000,
            retryAttempts: options.retryAttempts || 3,
            retryDelay: options.retryDelay || 1000,
            ...options
        };
    }

    /**
     * Set authentication token
     */
    setToken(token) {
        this.token = token;
        if (typeof localStorage !== 'undefined') {
            localStorage.setItem('monitoring-worker-token', token);
        }
    }

    /**
     * Get stored token
     */
    getToken() {
        if (!this.token && typeof localStorage !== 'undefined') {
            this.token = localStorage.getItem('monitoring-worker-token');
        }
        return this.token;
    }

    /**
     * Clear authentication token
     */
    clearToken() {
        this.token = null;
        if (typeof localStorage !== 'undefined') {
            localStorage.removeItem('monitoring-worker-token');
        }
    }

    /**
     * Make authenticated API call
     */
    async apiCall(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const token = this.getToken();
        
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                ...(token && { 'Authorization': `Bearer ${token}` })
            }
        };

        const requestOptions = { ...defaultOptions, ...options };
        if (options.headers) {
            requestOptions.headers = { ...defaultOptions.headers, ...options.headers };
        }

        let lastError;
        for (let attempt = 1; attempt <= this.options.retryAttempts; attempt++) {
            try {
                const response = await fetch(url, requestOptions);
                
                if (response.status === 401) {
                    this.clearToken();
                    throw new Error('Authentication required');
                }
                
                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(`HTTP ${response.status}: ${errorText}`);
                }
                
                const contentType = response.headers.get('content-type');
                if (contentType && contentType.includes('application/json')) {
                    return await response.json();
                } else {
                    return await response.text();
                }
            } catch (error) {
                lastError = error;
                if (attempt < this.options.retryAttempts && this.isRetryableError(error)) {
                    await this.delay(this.options.retryDelay * attempt);
                    continue;
                }
                break;
            }
        }
        
        throw lastError;
    }

    /**
     * Check if error is retryable
     */
    isRetryableError(error) {
        return error.message.includes('fetch') || 
               error.message.includes('network') ||
               error.message.includes('timeout');
    }

    /**
     * Delay utility
     */
    delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    // Authentication Methods
    async login(username, password) {
        const response = await this.apiCall('/api/auth/login', {
            method: 'POST',
            body: JSON.stringify({ username, password })
        });
        
        this.setToken(response.token);
        this.emit('authenticated', response);
        return response;
    }

    async logout() {
        try {
            await this.apiCall('/api/auth/logout', { method: 'POST' });
        } catch (error) {
            // Ignore logout errors
        }
        
        this.clearToken();
        await this.disconnectSignalR();
        this.stopAutoRefresh();
        this.emit('logout');
    }

    async validateToken() {
        try {
            const response = await this.apiCall('/api/auth/validate');
            return response.valid;
        } catch (error) {
            return false;
        }
    }

    // Worker Management Methods
    async getWorkerStatus() {
        const status = await this.apiCall('/api/worker/status');
        this.emit('statusUpdated', status);
        return status;
    }

    async startWorker() {
        const result = await this.apiCall('/api/worker/start', { method: 'POST' });
        this.emit('workerStarted', result);
        return result;
    }

    async stopWorker() {
        const result = await this.apiCall('/api/worker/stop', { method: 'POST' });
        this.emit('workerStopped', result);
        return result;
    }

    async restartWorker() {
        const result = await this.apiCall('/api/worker/restart', { method: 'POST' });
        this.emit('workerRestarted', result);
        return result;
    }

    async pauseJob(jobName) {
        const result = await this.apiCall(`/api/worker/jobs/${jobName}/pause`, { method: 'POST' });
        this.emit('jobPaused', { jobName, result });
        return result;
    }

    async resumeJob(jobName) {
        const result = await this.apiCall(`/api/worker/jobs/${jobName}/resume`, { method: 'POST' });
        this.emit('jobResumed', { jobName, result });
        return result;
    }

    // Monitoring Methods
    async getMonitoringStatus() {
        return await this.apiCall('/api/monitoring/status');
    }

    async getMonitoringMetrics() {
        return await this.apiCall('/api/monitoring/metrics');
    }

    async runManualCheck(endpointName = null) {
        const endpoint = endpointName ? `/api/monitoring/check/${endpointName}` : '/api/monitoring/check';
        const result = await this.apiCall(endpoint, { method: 'POST' });
        this.emit('manualCheckStarted', { endpointName, result });
        return result;
    }

    // Configuration Methods
    async getConfiguration() {
        return await this.apiCall('/api/configuration');
    }

    async updateConfiguration(config) {
        const result = await this.apiCall('/api/configuration', {
            method: 'PUT',
            body: JSON.stringify(config)
        });
        this.emit('configurationUpdated', result);
        return result;
    }

    // Database Monitoring Methods
    async getDatabaseConnections() {
        return await this.apiCall('/api/database/connections');
    }

    async testDatabaseConnection(connectionName) {
        return await this.apiCall(`/api/database/test/${connectionName}`, { method: 'POST' });
    }

    async executeDatabaseQuery(connectionName, query, parameters = {}) {
        return await this.apiCall(`/api/database/query/${connectionName}`, {
            method: 'POST',
            body: JSON.stringify({ query, parameters })
        });
    }

    // SignalR Methods
    async connectSignalR() {
        if (this.signalRConnection) {
            return;
        }

        if (typeof signalR === 'undefined') {
            throw new Error('SignalR library not loaded');
        }

        const token = this.getToken();
        if (!token) {
            throw new Error('Authentication token required for SignalR connection');
        }

        this.signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${this.baseUrl}/monitoringHub`, {
                accessTokenFactory: () => token
            })
            .withAutomaticReconnect()
            .build();

        // Set up event handlers
        this.signalRConnection.on('WorkerStarted', (event) => {
            this.emit('workerStartedEvent', event);
        });

        this.signalRConnection.on('WorkerStopped', (event) => {
            this.emit('workerStoppedEvent', event);
        });

        this.signalRConnection.on('MonitoringResult', (result) => {
            this.emit('monitoringResult', result);
        });

        this.signalRConnection.on('JobExecuted', (jobInfo) => {
            this.emit('jobExecuted', jobInfo);
        });

        this.signalRConnection.on('ConfigurationChanged', (config) => {
            this.emit('configurationChanged', config);
        });

        this.signalRConnection.onreconnecting(() => {
            this.emit('signalRReconnecting');
        });

        this.signalRConnection.onreconnected(() => {
            this.emit('signalRReconnected');
        });

        this.signalRConnection.onclose(() => {
            this.emit('signalRDisconnected');
        });

        await this.signalRConnection.start();
        this.emit('signalRConnected');
    }

    async disconnectSignalR() {
        if (this.signalRConnection) {
            await this.signalRConnection.stop();
            this.signalRConnection = null;
            this.emit('signalRDisconnected');
        }
    }

    // Auto-refresh Methods
    startAutoRefresh() {
        if (this.autoRefreshInterval) {
            return;
        }

        this.autoRefreshInterval = setInterval(async () => {
            try {
                await this.getWorkerStatus();
            } catch (error) {
                this.emit('autoRefreshError', error);
            }
        }, this.options.refreshInterval);

        this.emit('autoRefreshStarted');
    }

    stopAutoRefresh() {
        if (this.autoRefreshInterval) {
            clearInterval(this.autoRefreshInterval);
            this.autoRefreshInterval = null;
            this.emit('autoRefreshStopped');
        }
    }

    // Event Management
    on(event, handler) {
        if (!this.eventHandlers.has(event)) {
            this.eventHandlers.set(event, []);
        }
        this.eventHandlers.get(event).push(handler);
    }

    off(event, handler) {
        if (this.eventHandlers.has(event)) {
            const handlers = this.eventHandlers.get(event);
            const index = handlers.indexOf(handler);
            if (index > -1) {
                handlers.splice(index, 1);
            }
        }
    }

    emit(event, data) {
        if (this.eventHandlers.has(event)) {
            this.eventHandlers.get(event).forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error(`Error in event handler for ${event}:`, error);
                }
            });
        }
    }

    // Utility Methods
    async initialize() {
        const token = this.getToken();
        if (token) {
            const isValid = await this.validateToken();
            if (!isValid) {
                this.clearToken();
                throw new Error('Invalid authentication token');
            }
        }

        if (this.options.autoRefresh) {
            this.startAutoRefresh();
        }

        if (token) {
            await this.connectSignalR();
        }

        this.emit('initialized');
    }

    async destroy() {
        this.stopAutoRefresh();
        await this.disconnectSignalR();
        this.eventHandlers.clear();
        this.emit('destroyed');
    }

    // Health check utility
    async healthCheck() {
        try {
            const response = await fetch(`${this.baseUrl}/healthz/live`);
            return response.ok;
        } catch (error) {
            return false;
        }
    }
}

// Export for different module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = MonitoringWorkerClient;
} else if (typeof define === 'function' && define.amd) {
    define([], function() {
        return MonitoringWorkerClient;
    });
} else if (typeof window !== 'undefined') {
    window.MonitoringWorkerClient = MonitoringWorkerClient;
}

// Usage examples (commented out)
/*
// Basic usage
const client = new MonitoringWorkerClient('http://localhost:5002', {
    autoRefresh: true,
    refreshInterval: 30000
});

// Event handling
client.on('statusUpdated', (status) => {
    console.log('Worker status updated:', status);
});

client.on('monitoringResult', (result) => {
    console.log('Monitoring result:', result);
});

// Initialize and login
await client.initialize();
await client.login('admin', 'admin123');

// Worker management
const status = await client.getWorkerStatus();
await client.startWorker();
await client.stopWorker();

// Monitoring
await client.runManualCheck();
await client.runManualCheck('LocalTest');

// Cleanup
await client.destroy();
*/
