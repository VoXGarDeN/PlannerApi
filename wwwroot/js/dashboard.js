// Dashboard JavaScript
class DashboardManager {
    constructor() {
        this.stats = {};
        this.charts = {};
        this.updateInterval = null;
        this.initialize();
    }

    async initialize() {
        await this.loadStats();
        this.initializeCharts();
        this.setupEventListeners();
        this.startAutoRefresh();
        this.initializeAnimations();
    }

    async loadStats() {
        try {
            const response = await fetch('/Dashboard/GetStats');
            this.stats = await response.json();
            this.updateUI();
        } catch (error) {
            console.error('Error loading stats:', error);
            this.showError('Failed to load dashboard data');
        }
    }

    async loadAnalytics() {
        try {
            const response = await fetch('/Dashboard/GetAnalytics');
            return await response.json();
        } catch (error) {
            console.error('Error loading analytics:', error);
            return null;
        }
    }

    updateUI() {
        // Update stat cards
        this.updateStatCard('totalTasks', this.stats.totalTasks);
        this.updateStatCard('activeTasks', this.stats.activeTasks);
        this.updateStatCard('totalResources', this.stats.totalResources);
        this.updateStatCard('productivityScore', `${this.stats.productivityScore.toFixed(1)}%`);
        
        // Update progress bars
        this.updateProgressBars();
        
        // Update timeline
        this.updateTimeline();
    }

    updateStatCard(elementId, value) {
        const element = document.getElementById(elementId);
        if (element) {
            const oldValue = parseInt(element.textContent) || 0;
            this.animateNumberChange(element, oldValue, value);
        }
    }

    animateNumberChange(element, from, to) {
        const duration = 1000;
        const startTime = Date.now();
        
        function update() {
            const elapsed = Date.now() - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const easeProgress = this.easeOutCubic(progress);
            const current = Math.floor(from + (to - from) * easeProgress);
            
            element.textContent = current;
            
            if (progress < 1) {
                requestAnimationFrame(update);
            }
        }
        
        update();
    }

    easeOutCubic(t) {
        return 1 - Math.pow(1 - t, 3);
    }

    updateProgressBars() {
        const utilization = this.stats.resourcesUtilization || 0;
        const progressBars = document.querySelectorAll('.progress-bar-fill');
        
        progressBars.forEach(bar => {
            const targetWidth = bar.classList.contains('utilization') ? utilization : 75;
            this.animateProgressBar(bar, targetWidth);
        });
    }

    animateProgressBar(bar, targetWidth) {
        const currentWidth = parseFloat(bar.style.width) || 0;
        const duration = 1000;
        const startTime = Date.now();
        
        function update() {
            const elapsed = Date.now() - startTime;
            const progress = Math.min(elapsed / duration, 1);
            const easeProgress = this.easeOutCubic(progress);
            const width = currentWidth + (targetWidth - currentWidth) * easeProgress;
            
            bar.style.width = `${width}%`;
            bar.textContent = `${Math.round(width)}%`;
            
            if (progress < 1) {
                requestAnimationFrame(update);
            }
        }
        
        update();
    }

    initializeCharts() {
        // Task completion chart
        this.charts.completion = new Chart(document.getElementById('completionChart'), {
            type: 'line',
            data: {
                labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
                datasets: [{
                    label: 'Completed Tasks',
                    data: [12, 19, 8, 15, 22, 18, 25],
                    borderColor: '#10b981',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    tension: 0.4,
                    fill: true
                }, {
                    label: 'Total Tasks',
                    data: [20, 25, 18, 30, 28, 24, 35],
                    borderColor: '#6366f1',
                    backgroundColor: 'rgba(99, 102, 241, 0.1)',
                    tension: 0.4,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: {
                        position: 'top',
                    }
                }
            }
        });

        // Peak hours chart
        this.charts.peakHours = new Chart(document.getElementById('peakHoursChart'), {
            type: 'bar',
            data: {
                labels: ['6AM', '9AM', '12PM', '3PM', '6PM', '9PM'],
                datasets: [{
                    label: 'Active Shifts',
                    data: [5, 12, 18, 20, 15, 8],
                    backgroundColor: 'rgba(99, 102, 241, 0.8)',
                    borderColor: '#6366f1',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Number of Shifts'
                        }
                    }
                }
            }
        });
    }

    setupEventListeners() {
        // Tab switching
        document.querySelectorAll('.tab-btn').forEach(btn => {
            btn.addEventListener('click', (e) => this.switchTab(e.target));
        });

        // Refresh button
        document.querySelector('.btn-refresh').addEventListener('click', () => this.refresh());

        // Quick action buttons
        document.querySelectorAll('.action-btn').forEach(btn => {
            btn.addEventListener('click', (e) => this.handleQuickAction(e.target));
        });

        // Stat card clicks
        document.querySelectorAll('.stat-card').forEach(card => {
            card.addEventListener('click', (e) => this.handleStatCardClick(e.target.closest('.stat-card')));
        });
    }

    switchTab(button) {
        const tabId = button.getAttribute('data-tab');
        
        // Update active states
        document.querySelectorAll('.tab-btn').forEach(btn => btn.classList.remove('active'));
        document.querySelectorAll('.tab-pane').forEach(pane => pane.classList.remove('active'));
        
        button.classList.add('active');
        document.getElementById(tabId).classList.add('active');
        
        // Load tab-specific data
        this.loadTabData(tabId);
        
        // Animation
        this.animateTabSwitch(tabId);
    }

    async loadTabData(tabId) {
        switch(tabId) {
            case 'analytics':
                await this.loadAnalyticsData();
                break;
            case 'tasks':
                await this.loadTasksData();
                break;
            case 'resources':
                await this.loadResourcesData();
                break;
            case 'schedule':
                await this.loadScheduleData();
                break;
        }
    }

    async loadAnalyticsData() {
        const analytics = await this.loadAnalytics();
        if (analytics) {
            this.updateAnalyticsCharts(analytics);
        }
    }

    updateAnalyticsCharts(analytics) {
        // Update daily tasks chart
        if (this.charts.dailyTasks) {
            this.charts.dailyTasks.data.datasets[0].data = Object.values(analytics.dailyTasks).map(d => d.count);
            this.charts.dailyTasks.data.datasets[1].data = Object.values(analytics.dailyTasks).map(d => d.completed);
            this.charts.dailyTasks.update();
        }

        // Update status distribution chart
        if (this.charts.statusDistribution) {
            this.charts.statusDistribution.data.datasets[0].data = Object.values(analytics.taskStatusDistribution);
            this.charts.statusDistribution.update();
        }
    }

    animateTabSwitch(tabId) {
        const tabPane = document.getElementById(tabId);
        tabPane.style.animation = 'none';
        setTimeout(() => {
            tabPane.style.animation = 'slideInRight 0.5s ease';
        }, 10);
    }

    handleQuickAction(button) {
        const action = button.closest('.action-btn').classList[1];
        
        switch(action) {
            case 'btn-create-task':
                this.createTask();
                break;
            case 'btn-schedule-shift':
                this.scheduleShift();
                break;
            case 'btn-generate-report':
                this.generateReport();
                break;
            case 'btn-notifications':
                this.showNotifications();
                break;
        }
    }

    async createTask() {
        // Show modal or redirect to task creation
        this.showNotification('Creating new task...', 'info');
        // Implementation would open a modal or navigate to task creation page
    }

    async scheduleShift() {
        this.showNotification('Opening shift scheduler...', 'info');
        // Implementation would open scheduling interface
    }

    async generateReport() {
        this.showNotification('Generating report...', 'info');
        // Implementation would trigger report generation
    }

    showNotifications() {
        this.showNotification('Loading notifications...', 'info');
        // Implementation would show notifications panel
    }

    handleStatCardClick(card) {
        const cardType = card.classList[1];
        
        switch(cardType) {
            case 'stat-total-tasks':
                this.switchTab(document.querySelector('[data-tab="tasks"]'));
                break;
            case 'stat-active-tasks':
                this.switchTab(document.querySelector('[data-tab="schedule"]'));
                break;
            case 'stat-resources':
                this.switchTab(document.querySelector('[data-tab="resources"]'));
                break;
            case 'stat-productivity':
                this.switchTab(document.querySelector('[data-tab="analytics"]'));
                break;
        }
        
        // Animation
        this.animateCardClick(card);
    }

    animateCardClick(card) {
        card.style.animation = 'pulse 0.5s';
        setTimeout(() => {
            card.style.animation = '';
        }, 500);
    }

    async refresh() {
        // Show loading state
        document.querySelector('.dashboard-container').classList.add('refreshing');
        
        await this.loadStats();
        
        // Hide loading state
        setTimeout(() => {
            document.querySelector('.dashboard-container').classList.remove('refreshing');
            this.showNotification('Dashboard refreshed', 'success');
        }, 500);
    }

    startAutoRefresh() {
        // Refresh every 5 minutes
        this.updateInterval = setInterval(() => {
            this.loadStats();
        }, 5 * 60 * 1000);
    }

    initializeAnimations() {
        // Add hover animations to all cards
        const cards = document.querySelectorAll('.stat-card, .overview-card, .analytics-card');
        cards.forEach(card => {
            card.addEventListener('mouseenter', () => {
                card.style.transform = 'translateY(-5px)';
                card.style.boxShadow = 'var(--shadow-lg)';
            });
            
            card.addEventListener('mouseleave', () => {
                card.style.transform = '';
                card.style.boxShadow = '';
            });
        });

        // Add ripple effect to buttons
        document.querySelectorAll('button').forEach(button => {
            button.addEventListener('click', function(e) {
                const ripple = document.createElement('span');
                const rect = this.getBoundingClientRect();
                const size = Math.max(rect.width, rect.height);
                const x = e.clientX - rect.left - size / 2;
                const y = e.clientY - rect.top - size / 2;
                
                ripple.style.cssText = `
                    position: absolute;
                    border-radius: 50%;
                    background: rgba(255, 255, 255, 0.7);
                    transform: scale(0);
                    animation: ripple 0.6s linear;
                    width: ${size}px;
                    height: ${size}px;
                    left: ${x}px;
                    top: ${y}px;
                `;
                
                this.appendChild(ripple);
                
                setTimeout(() => ripple.remove(), 600);
            });
        });
    }

    showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="notification-icon">${this.getNotificationIcon(type)}</i>
                <span>${message}</span>
            </div>
            <button class="notification-close" onclick="this.parentElement.remove()">×</button>
        `;
        
        document.body.appendChild(notification);
        
        // Auto remove after 5 seconds
        setTimeout(() => {
            if (notification.parentElement) {
                notification.remove();
            }
        }, 5000);
    }

    getNotificationIcon(type) {
        const icons = {
            success: '✅',
            error: '❌',
            warning: '⚠️',
            info: 'ℹ️'
        };
        return icons[type] || icons.info;
    }

    updateTimeline() {
        // Sample timeline data - in real app, this would come from API
        const timelineItems = [
            { time: '10:30 AM', task: 'Team meeting', status: 'completed' },
            { time: '11:45 AM', task: 'Code review', status: 'in-progress' },
            { time: '1:30 PM', task: 'Client call', status: 'upcoming' },
            { time: '3:00 PM', task: 'Project planning', status: 'upcoming' }
        ];

        const timelineContainer = document.querySelector('.timeline-items');
        if (timelineContainer) {
            timelineContainer.innerHTML = timelineItems.map(item => `
                <div class="timeline-item ${item.status}">
                    <div class="timeline-time">${item.time}</div>
                    <div class="timeline-content">
                        <div class="timeline-task">${item.task}</div>
                        <div class="timeline-status">${item.status}</div>
                    </div>
                </div>
            `).join('');
        }
    }
}

// Initialize dashboard when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    window.dashboard = new DashboardManager();
});

// CSS for ripple effect
const style = document.createElement('style');
style.textContent = `
    @keyframes ripple {
        to {
            transform: scale(4);
            opacity: 0;
        }
    }
    
    button {
        position: relative;
        overflow: hidden;
    }
`;
document.head.appendChild(style);