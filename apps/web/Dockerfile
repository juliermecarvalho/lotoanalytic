FROM node:24-alpine AS build
WORKDIR /app

ARG VITE_API_BASE_URL=http://localhost:5291
ARG VITE_KEYCLOAK_AUTHORITY=http://localhost:8080/realms/lotoanalytics
ARG VITE_KEYCLOAK_CLIENT_ID=lotoanalytics-web

ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_KEYCLOAK_AUTHORITY=$VITE_KEYCLOAK_AUTHORITY
ENV VITE_KEYCLOAK_CLIENT_ID=$VITE_KEYCLOAK_CLIENT_ID

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
