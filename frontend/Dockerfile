FROM node:18-bullseye AS build
WORKDIR /app

# Install dependencies and build
COPY package*.json ./
RUN npm ci --production=false
COPY . .
ARG BASE_HREF=/
RUN npm run build -- --configuration production --base-href $BASE_HREF

FROM nginx:alpine

# Copy built files from Angular build stage
COPY --from=build /app/dist/* /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s CMD wget --no-verbose --tries=1 --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
