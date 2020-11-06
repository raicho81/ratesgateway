DROP TABLE IF EXISTS "ExchangeRates";
DROP SEQUENCE IF EXISTS "ExchangeRates_ExchangeRatesId_seq";
CREATE SEQUENCE "ExchangeRates_ExchangeRatesId_seq" START 1 ;

CREATE TABLE "public"."ExchangeRates" (
    "ExchangeRatesId" integer DEFAULT nextval('"ExchangeRates_ExchangeRatesId_seq"') NOT NULL,
    "Timestamp" timestamp NOT NULL,
    "Base" character varying(3) NOT NULL,
    CONSTRAINT "ExchangeRates_ExchangeRatesId" PRIMARY KEY ("ExchangeRatesId")
) WITH (oids = false);

CREATE INDEX "ExchangeRates_Base" ON "public"."ExchangeRates" USING btree ("Base");

CREATE INDEX "ExchangeRates_Timestamp" ON "public"."ExchangeRates" USING btree ("Timestamp");


DROP TABLE IF EXISTS "Rates";
DROP SEQUENCE IF EXISTS "Rates_RateId_seq";
CREATE SEQUENCE "Rates_RateId_seq" START 1;

CREATE TABLE "public"."Rates" (
    "RateId" integer DEFAULT nextval('"Rates_RateId_seq"') NOT NULL,
    "ExchangeRatesId" integer NOT NULL,
    "Symbol" character(3) NOT NULL,
    "RateValue" double precision NOT NULL,
    CONSTRAINT "Rates_RateId" PRIMARY KEY ("RateId"),
    CONSTRAINT "Rates_ExchangeRatesId_fkey" FOREIGN KEY ("ExchangeRatesId") REFERENCES "ExchangeRates"("ExchangeRatesId") ON UPDATE CASCADE ON DELETE CASCADE
) WITH (oids = false);

DROP TABLE IF EXISTS "Stats";
DROP SEQUENCE IF EXISTS "Stats_Id_seq";
CREATE SEQUENCE "Stats_Id_seq" START 1 ;

CREATE TABLE "public"."Stats" (
    "Id" integer DEFAULT nextval('"Stats_Id_seq"') NOT NULL,
    "ServiceName" character varying(255) NOT NULL,
    "RequestId" uuid NOT NULL,
    "ClientId" character varying(100) NOT NULL,
    CONSTRAINT "Stats_Id" PRIMARY KEY ("Id")
) WITH (oids = false);
