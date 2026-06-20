# SS14.Web SSO Configuration Example

# Add these sections to your appsettings.Secret.yml

# VK OAuth Configuration
Vkontakte:
  ClientId: "your_vk_app_id"
  ClientSecret: "your_vk_app_secret"

# Yandex OAuth Configuration  
Yandex:
  ClientId: "your_yandex_client_id"
  ClientSecret: "your_yandex_client_secret"

# How to get credentials:

# VK:
# 1. Go to https://dev.vk.com/
# 2. Create a new application (Standalone application)
# 3. In Settings, add redirect URI: https://your-domain.com/signin-vkontakte
# 4. Copy App ID (ClientId) and Secure key (ClientSecret)
# 5. Enable "Email" and "Birthday" in permissions

# Yandex:
# 1. Go to https://oauth.yandex.ru/
# 2. Register new application
# 3. In Platforms, add "Web services" with callback URL: https://your-domain.com/signin-yandex
# 4. In Data access, enable "login:email" and "login:birthday"
# 5. Copy Client ID and Client secret

# Database Migration:
# Run this SQL migration on your PostgreSQL database:
# ALTER TABLE "AspNetUsers" ADD COLUMN "Birthday" timestamp with time zone NOT NULL DEFAULT '1000-01-01 00:00:00+00';
