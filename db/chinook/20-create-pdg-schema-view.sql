CREATE SCHEMA IF NOT EXISTS pdg
    AUTHORIZATION chinook_owner;

REVOKE ALL ON SCHEMA pdg FROM PUBLIC;

CREATE OR REPLACE VIEW pdg.sales_summary AS
SELECT
    c.customer_id AS "CustomerId",
    i.billing_country AS "Country",
    i.invoice_date AS "InvoiceDate",
    i.total AS "Total"
FROM public.customer AS c
JOIN public.invoice AS i
    ON i.customer_id = c.customer_id;