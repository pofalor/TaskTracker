FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.27-alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
RUN rm -rf /usr/share/nginx/html/*
COPY --from=build /app/dist/task-tracker/browser /usr/share/nginx/html
# SSR-сборка с prerender кладёт клиентский индекс как index.csr.html — nginx ждёт index.html
RUN [ -f /usr/share/nginx/html/index.html ] || cp /usr/share/nginx/html/index.csr.html /usr/share/nginx/html/index.html
EXPOSE 80
