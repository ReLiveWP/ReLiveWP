-- Runs once on first initialisation of the postgres volume (docker-entrypoint-initdb.d).
-- One database per data-bearing service; all owned by the POSTGRES_USER (relive).
CREATE DATABASE relive_identity;
CREATE DATABASE relive_connectedservices;
CREATE DATABASE relive_deviceregistration;
CREATE DATABASE relive_skybox;
CREATE DATABASE relive_skydrive;
CREATE DATABASE relive_push;
