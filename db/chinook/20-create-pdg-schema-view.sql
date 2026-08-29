CREATE SCHEMA IF NOT EXISTS pdg
    AUTHORIZATION chinook_owner;

REVOKE ALL ON SCHEMA pdg FROM PUBLIC;

CREATE OR REPLACE VIEW pdg.sales_summary AS
SELECT
    c."CustomerId" AS "CustomerId",
    i."BillingCountry" AS "Country",
    i."InvoiceDate" AS "InvoiceDate",
    i."Total" AS "Total"
FROM public."Customer" AS c
JOIN public."Invoice" AS i
    ON i."CustomerId" = c."CustomerId";