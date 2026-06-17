# Etap 1: Build aplikacji React/Vite
FROM node:20-alpine AS build

WORKDIR /app

# Kopiuj package.json i package-lock.json
COPY takstracker.client/package*.json ./

# Zainstaluj zależności
RUN npm ci

# Kopiuj resztę kodu źródłowego
COPY takstracker.client/ ./

# Zbuduj aplikację produkcyjną
RUN npm run build

# Etap 2: Serwowanie przez Nginx
FROM nginx:alpine

# Kopiuj zbudowaną aplikację do katalogu Nginx
COPY --from=build /app/dist /usr/share/nginx/html

# Kopiuj konfigurację Nginx
COPY takstracker.client/nginx.conf /etc/nginx/conf.d/default.conf

# Eksponuj port 80
EXPOSE 80

# Nginx uruchomi się automatycznie
CMD ["nginx", "-g", "daemon off;"]
