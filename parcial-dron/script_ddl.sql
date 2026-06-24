-- =============================================================
-- PARTE A: DISEÑO DE BASE DE DATOS
-- Script DDL para el Simulador de Trayectoria de Dron
-- Base de datos: dron_db (PostgreSQL)
-- =============================================================

-- Crear la base de datos (ejecutar conectado a 'postgres' primero)
-- CREATE DATABASE dron_db;

-- Tabla maestra: almacena la cabecera de cada ejecución exitosa
CREATE TABLE IF NOT EXISTS tb_master_control (
    id          SERIAL          PRIMARY KEY,
    fecha       TIMESTAMP       NOT NULL DEFAULT NOW(),
    n           INTEGER         NOT NULL,
    coord_x     INTEGER         NOT NULL,
    coord_y     INTEGER         NOT NULL
);

-- Tabla de detalle: almacena el rastro de movimientos de cada ejecución
CREATE TABLE IF NOT EXISTS tb_det_log (
    id              SERIAL      PRIMARY KEY,
    id_master       INTEGER     NOT NULL,
    nro_paso        INTEGER     NOT NULL,
    coord_x         INTEGER     NOT NULL,
    coord_y         INTEGER     NOT NULL,
    CONSTRAINT fk_det_master
        FOREIGN KEY (id_master)
        REFERENCES tb_master_control(id)
        ON DELETE CASCADE
);