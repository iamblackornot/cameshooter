module.exports = {
  apps: [{
    name: 'cameshooter',
    script: 'dotnet',
    args: 'cameshooter.dll',
    cwd: 'bin/Release/net8.0/c',
    instances: 1,
    autorestart: true,
    watch: false,
    max_memory_restart: '256M',
    env: {
      ASPNETCORE_ENVIRONMENT: 'Production'
    },
    error_file: './logs/err.log',
    out_file: './logs/out.log',
    log_file: './logs/combined.log',
    time: true
  }]
}