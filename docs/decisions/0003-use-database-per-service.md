# ADR 0003: Database per Service

## Status

Aceito

## Decisão

Cada serviço com persistência própria: `catalog_db`, `ordering_db`, Redis para Basket.

## Regra

Nenhum serviço acessa diretamente o banco de outro.
