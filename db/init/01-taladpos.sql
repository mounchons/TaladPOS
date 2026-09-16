--
-- PostgreSQL database dump
--


-- Dumped from database version 16.10 (Debian 16.10-1.pgdg13+1)
-- Dumped by pg_dump version 16.10 (Debian 16.10-1.pgdg13+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

ALTER TABLE IF EXISTS ONLY public.sale_line_items DROP CONSTRAINT IF EXISTS "FK_sale_line_items_sales_SaleId";
ALTER TABLE IF EXISTS ONLY public.sale_applied_promotions DROP CONSTRAINT IF EXISTS "FK_sale_applied_promotions_sales_SaleId";
ALTER TABLE IF EXISTS ONLY public.conditional_promotion_lines DROP CONSTRAINT IF EXISTS "FK_conditional_promotion_lines_conditional_promotions_Conditio~";
DROP INDEX IF EXISTS public."IX_staff_Username";
DROP INDEX IF EXISTS public."IX_sale_line_items_SaleId";
DROP INDEX IF EXISTS public."IX_sale_applied_promotions_SaleId";
DROP INDEX IF EXISTS public."IX_products_Barcode";
DROP INDEX IF EXISTS public."IX_members_PhoneNumber";
DROP INDEX IF EXISTS public."IX_conditional_promotion_lines_ConditionalPromotionId_ProductId";
ALTER TABLE IF EXISTS ONLY public.staff DROP CONSTRAINT IF EXISTS "PK_staff";
ALTER TABLE IF EXISTS ONLY public.sales DROP CONSTRAINT IF EXISTS "PK_sales";
ALTER TABLE IF EXISTS ONLY public.sale_line_items DROP CONSTRAINT IF EXISTS "PK_sale_line_items";
ALTER TABLE IF EXISTS ONLY public.sale_applied_promotions DROP CONSTRAINT IF EXISTS "PK_sale_applied_promotions";
ALTER TABLE IF EXISTS ONLY public.promotions DROP CONSTRAINT IF EXISTS "PK_promotions";
ALTER TABLE IF EXISTS ONLY public.products DROP CONSTRAINT IF EXISTS "PK_products";
ALTER TABLE IF EXISTS ONLY public.members DROP CONSTRAINT IF EXISTS "PK_members";
ALTER TABLE IF EXISTS ONLY public.conditional_promotions DROP CONSTRAINT IF EXISTS "PK_conditional_promotions";
ALTER TABLE IF EXISTS ONLY public.conditional_promotion_lines DROP CONSTRAINT IF EXISTS "PK_conditional_promotion_lines";
ALTER TABLE IF EXISTS ONLY public."__EFMigrationsHistory" DROP CONSTRAINT IF EXISTS "PK___EFMigrationsHistory";
DROP TABLE IF EXISTS public.staff;
DROP TABLE IF EXISTS public.sales;
DROP TABLE IF EXISTS public.sale_line_items;
DROP TABLE IF EXISTS public.sale_applied_promotions;
DROP TABLE IF EXISTS public.promotions;
DROP TABLE IF EXISTS public.products;
DROP TABLE IF EXISTS public.members;
DROP TABLE IF EXISTS public.conditional_promotions;
DROP TABLE IF EXISTS public.conditional_promotion_lines;
DROP TABLE IF EXISTS public."__EFMigrationsHistory";
SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: conditional_promotion_lines; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.conditional_promotion_lines (
    "Id" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "MinimumQuantity" integer NOT NULL,
    "SortOrder" integer NOT NULL,
    "ConditionalPromotionId" uuid NOT NULL
);


--
-- Name: conditional_promotions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.conditional_promotions (
    "Id" uuid NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Reward_Kind" character varying(10) NOT NULL,
    "Reward_GiftProductId" uuid,
    "Reward_GiftQuantity" integer,
    "Reward_DiscountPercentage" numeric(5,2),
    "AppliesToMembersOnly" boolean NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL
);


--
-- Name: members; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.members (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "PhoneNumber" character varying(30) NOT NULL,
    "AccumulatedPurchaseTotal" numeric(12,2) NOT NULL
);


--
-- Name: products; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.products (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "ImageUrl" text NOT NULL,
    "Price" numeric(12,2) NOT NULL,
    "Barcode" character varying(64),
    "StockQuantity" integer NOT NULL,
    "LowStockThreshold" integer NOT NULL
);


--
-- Name: promotions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.promotions (
    "Id" uuid NOT NULL,
    "Scope" character varying(10) NOT NULL,
    "DiscountPercentage" numeric(5,2) NOT NULL,
    "ProductId" uuid,
    "AppliesToMembersOnly" boolean NOT NULL,
    "StartDate" date NOT NULL,
    "EndDate" date NOT NULL
);


--
-- Name: sale_applied_promotions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sale_applied_promotions (
    "Id" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "PromotionId" uuid NOT NULL,
    "DescriptionSnapshot" character varying(400) NOT NULL,
    "SetCount" integer NOT NULL,
    "DiscountAmount" numeric(12,2) NOT NULL
);


--
-- Name: sale_line_items; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sale_line_items (
    "Id" uuid NOT NULL,
    "SaleId" uuid NOT NULL,
    "ProductId" uuid NOT NULL,
    "ProductNameSnapshot" character varying(200) NOT NULL,
    "UnitPriceSnapshot" numeric(12,2) NOT NULL,
    "Quantity" integer NOT NULL,
    "DiscountAmount" numeric(12,2) NOT NULL,
    "IsGift" boolean DEFAULT false NOT NULL
);


--
-- Name: sales; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sales (
    "Id" uuid NOT NULL,
    "StaffId" uuid NOT NULL,
    "MemberId" uuid,
    "CreatedAtUtc" timestamp with time zone NOT NULL
);


--
-- Name: staff; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.staff (
    "Id" uuid NOT NULL,
    "Name" character varying(200) NOT NULL,
    "Username" character varying(100) NOT NULL,
    "PasswordHash" text NOT NULL,
    "Role" character varying(20) NOT NULL
);


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20260910053505_InitialCreate	8.0.10
20260910054406_AddSales	8.0.10
20260910063302_AddMembers	8.0.10
20260910065827_AddPromotions	8.0.10
20260916073309_AddConditionalPromotions	8.0.10
\.


--
-- Data for Name: conditional_promotion_lines; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.conditional_promotion_lines ("Id", "ProductId", "MinimumQuantity", "SortOrder", "ConditionalPromotionId") FROM stdin;
55476495-78e8-48ae-9c20-31807ae0b973	e1a8f3f5-cf72-4701-88cc-0635a07b876b	1	1	2978f07a-859a-4f9c-a3f8-152892ac4cf9
7f13c8e6-3261-4fd6-b8dd-b7ba86f5b97f	4036fb39-a055-4bf7-b14b-3890d5086085	1	0	06400fa1-3365-4c50-b944-2b3b9b61257b
83e2e899-ff0f-40af-9d30-eca341d68e90	39800e29-c024-4057-94f9-900b83b2cfb9	1	1	06400fa1-3365-4c50-b944-2b3b9b61257b
c0a181b7-039a-4d7c-a136-0fc1ac5cf301	4036fb39-a055-4bf7-b14b-3890d5086085	1	0	2978f07a-859a-4f9c-a3f8-152892ac4cf9
eb9511e8-c849-4b2a-9150-5fa6e0cb4293	4036fb39-a055-4bf7-b14b-3890d5086085	2	0	236dc3ad-6a7d-447a-8527-66ed559dbe8e
\.


--
-- Data for Name: conditional_promotions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.conditional_promotions ("Id", "Name", "Reward_Kind", "Reward_GiftProductId", "Reward_GiftQuantity", "Reward_DiscountPercentage", "AppliesToMembersOnly", "StartDate", "EndDate") FROM stdin;
06400fa1-3365-4c50-b944-2b3b9b61257b	ซื้อคู่ลด 15%	Percentage	\N	\N	15.00	f	2026-09-06	2026-10-16
236dc3ad-6a7d-447a-8527-66ed559dbe8e	มะม่วงซื้อ 2 แถม 1	Gift	4036fb39-a055-4bf7-b14b-3890d5086085	1	\N	f	2026-09-06	2026-10-16
2978f07a-859a-4f9c-a3f8-152892ac4cf9	ซื้อคู่แถมส้มโอ	Gift	6f0171cb-68d4-4e72-aacb-f9579174791b	1	\N	f	2026-09-06	2026-10-16
\.


--
-- Data for Name: members; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.members ("Id", "Name", "PhoneNumber", "AccumulatedPurchaseTotal") FROM stdin;
0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	สมหญิง รักษ์ไทย	0898765432	2922.80
47b0ab78-bd86-4bd4-a734-db45272c8133	มาลี ศรีสุข	0823334444	3394.50
cbc5eca9-9d48-47bd-830d-13c9ee5feb6d	ประเสริฐ ทองดี	0845556666	0.00
ede2e7fc-31ef-403e-8f4b-02484e2d08bd	สมชาย ใจดี	0812345678	4581.55
f4f1c31b-70e0-47eb-9a83-18cdbce54f63	วิชัย มั่นคง	0861112222	2406.00
\.


--
-- Data for Name: products; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.products ("Id", "Name", "ImageUrl", "Price", "Barcode", "StockQuantity", "LowStockThreshold") FROM stdin;
0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	http://localhost:3000/images/products/orange-512.png	15.00	8851234000036	60	5
1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	http://localhost:3000/images/products/kiwi-512.png	20.00	8851234000203	1	5
35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	http://localhost:3000/images/products/coconut-512.png	35.00	\N	30	5
360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	http://localhost:3000/images/products/pineapple-512.png	35.00	8851234000067	22	5
39800e29-c024-4057-94f9-900b83b2cfb9	ทุเรียน	http://localhost:3000/images/products/durian-512.png	350.00	\N	0	5
4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	http://localhost:3000/images/products/mango-512.png	45.00	8851234000043	30	5
68bd5af1-2587-48a6-9592-788577cde3fb	ลิ้นจี่	http://localhost:3000/images/products/lychee-512.png	90.00	8851234000166	12	5
6f0171cb-68d4-4e72-aacb-f9579174791b	ส้มโอ	http://localhost:3000/images/products/pomelo-512.png	120.00	\N	15	5
7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	http://localhost:3000/images/products/papaya-512.png	30.00	8851234000098	26	5
8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	http://localhost:3000/images/products/strawberry-512.png	150.00	8851234000081	0	5
8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	http://localhost:3000/images/products/banana-512.png	45.00	8851234000029	35	5
90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	http://localhost:3000/images/products/rambutan-512.png	50.00	8851234000142	5	5
a11a3213-9d2f-4ce0-809a-223692bc89e5	ลูกแพร์	http://localhost:3000/images/products/pear-512.png	30.00	8851234000197	20	5
bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	http://localhost:3000/images/products/guava-512.png	20.00	8851234000104	40	5
c2ef1b09-710a-4c4c-844e-dbb6bde791d5	มังคุด	http://localhost:3000/images/products/mangosteen-512.png	80.00	8851234000135	4	5
e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	http://localhost:3000/images/products/watermelon-512.png	89.00	8851234000050	8	10
e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	http://localhost:3000/images/products/apple-512.png	25.00	8851234000012	48	5
eecea84e-09c5-4ea5-8a73-0f30d8b9f38d	แก้วมังกร	http://localhost:3000/images/products/dragon-fruit-512.png	40.00	8851234000111	3	5
fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	http://localhost:3000/images/products/longan-512.png	70.00	8851234000159	25	5
fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	http://localhost:3000/images/products/grapes-512.png	120.00	8851234000074	18	5
\.


--
-- Data for Name: promotions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.promotions ("Id", "Scope", "DiscountPercentage", "ProductId", "AppliesToMembersOnly", "StartDate", "EndDate") FROM stdin;
3ec378e5-9e40-436b-bf0f-7dd0fbf198bf	Bill	20.00	\N	f	2026-09-23	2026-10-07
5d3b1e29-fd5a-4478-9ae4-d9547f55ad02	Item	20.00	39800e29-c024-4057-94f9-900b83b2cfb9	f	2026-08-02	2026-08-16
6bff653f-8d4e-4c9e-983a-fa1e8b967987	Bill	10.00	\N	t	2026-08-17	2026-10-16
71a2dace-18b3-4e8c-b90e-73b53b3cf361	Item	25.00	4036fb39-a055-4bf7-b14b-3890d5086085	t	2026-09-11	2026-09-21
8ed944c8-8b39-4817-97c0-9eb6951bbaa7	Item	15.00	e1a8f3f5-cf72-4701-88cc-0635a07b876b	t	2026-09-06	2026-09-30
920833f3-930a-4432-900b-0fced272e8db	Bill	5.00	\N	f	2026-08-07	2026-08-26
d0c5fa2e-a731-45f4-bc05-378773902e8f	Item	30.00	6f0171cb-68d4-4e72-aacb-f9579174791b	f	2026-09-19	2026-09-26
fa010def-b1de-4959-9fec-7b2cc2426569	Item	10.00	4036fb39-a055-4bf7-b14b-3890d5086085	f	2026-08-27	2026-09-26
\.


--
-- Data for Name: sale_applied_promotions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.sale_applied_promotions ("Id", "SaleId", "PromotionId", "DescriptionSnapshot", "SetCount", "DiscountAmount") FROM stdin;
\.


--
-- Data for Name: sale_line_items; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.sale_line_items ("Id", "SaleId", "ProductId", "ProductNameSnapshot", "UnitPriceSnapshot", "Quantity", "DiscountAmount", "IsGift") FROM stdin;
001a8395-54f1-490b-9929-23defbacbce3	46e4c058-dab3-4904-b962-dd21d188ca7e	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	2.25	f
013e9182-bfb7-4be9-ab96-31c9a4f0e645	befbc1f2-c375-4972-8bcb-ae2ff34d3f71	eecea84e-09c5-4ea5-8a73-0f30d8b9f38d	แก้วมังกร	40.00	3	6.00	f
0299d94d-17e8-4236-a7e4-5f89ec19ee55	13fcd4b2-40d4-4dc2-a408-8c33ddfa7c1e	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	1	0.00	f
04195100-fd9f-4a6e-ba93-620e2a4a984e	02777b26-91b2-44da-a216-237509a7b109	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	2	0.00	f
06e14019-2745-42d5-aedc-a792e07cefb5	eed80978-92d1-432f-a744-6e7ee7e0878a	7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	30.00	3	0.00	f
06ecdf1b-5e02-4471-a321-1e24a7c2e5ee	81013683-550f-4048-8167-8af278ab15d1	90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	50.00	3	7.50	f
07103dd3-b6cc-4f5c-851b-9f40563cae4a	73772ea6-fed8-49d6-8158-f93f9832803e	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	3	0.00	f
07f80cd6-0920-459d-a1ff-26935b155a45	28101201-af86-4dab-9ee6-af9b0e9c5d1d	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	2	7.00	f
0810662d-e385-431b-8542-f36b9a283510	29c40020-fba3-466b-a2d4-e2201d1dea76	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	3	6.00	f
0997a0bb-f92c-43b6-abb0-3f6dab3cea86	40037ad2-8bf2-4b60-8c23-e16348446b07	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	1	2.50	f
0ab99d64-bbe2-4dad-8adc-f98a51d6d7ad	1a4a454d-a96e-4247-a23b-130ec1b7a322	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	2	8.90	f
0b5f90a0-c132-41c3-91e9-1e2ec151e440	d112e2a7-95a0-4452-974f-b7c9aa61bcfe	c2ef1b09-710a-4c4c-844e-dbb6bde791d5	มังคุด	80.00	1	0.00	f
0ca90fe2-d4d0-4017-b7e4-e3bec7dc4ab1	1d0c79e2-504c-46a8-8837-aa95b1af3a76	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	2	7.00	f
0d9d4a88-3394-431b-b5a5-e13a0276db23	f7ba1cf1-8a25-4f71-b9c3-aa1a51c148f6	6f0171cb-68d4-4e72-aacb-f9579174791b	ส้มโอ	120.00	2	0.00	f
11697c91-6b42-48c1-aa79-e4604e97e0e4	acca3745-12c7-4a11-9403-793d553028bb	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	1	1.75	f
11706de1-2886-4b9e-83e9-0efa0d90598e	f73e9f70-39fb-431b-81f6-2f0ffb0b194d	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	2	0.00	f
12d14c5b-158d-4fae-9c26-b3f74ae6caee	eac6d959-573d-49ef-ac29-284270011fcf	a11a3213-9d2f-4ce0-809a-223692bc89e5	ลูกแพร์	30.00	2	6.00	f
13ff60de-1783-4a90-95a0-57ef1651f947	57a2bda9-8dfc-4ae8-af4e-4c45298b4083	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	1	1.00	f
14e3dfe1-5fc9-49b7-bab6-56e7af9cf27c	b0908038-6495-48d3-9e3f-21863d908697	90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	50.00	1	5.00	f
156dc78e-1497-4ea9-8d72-71ff7879fe33	f442a4d5-df0b-4228-afd0-be38f995446e	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	3	6.00	f
1657bd1a-b7a1-494a-bcde-8ba89a75282c	4af56563-e50d-4126-b1c6-fe830cd9ef90	90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	50.00	3	7.50	f
16d9e625-93dd-4699-916b-33e6317c89e5	5b50a668-1682-4d59-8c27-12394cc83798	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	1	1.75	f
173bdf31-9e98-4c7e-b64f-5c522748a362	c8fc8054-13d1-4a73-bac8-db263083e7e3	90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	50.00	3	0.00	f
1760d58d-1409-494b-959f-5083ddd9cd2e	6e41f83c-3aa4-474f-bc6b-a3dc342c3e9d	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	3	10.50	f
18a272a6-92b1-4cf1-bd1a-5dc4d93985b9	8597fb72-c80d-4eff-b6b7-02fa4872cd76	39800e29-c024-4057-94f9-900b83b2cfb9	ทุเรียน	350.00	3	105.00	f
1977e123-e40c-4f7c-b0c7-aaf50198036e	4af56563-e50d-4126-b1c6-fe830cd9ef90	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	3	6.75	f
1ae50e1a-afee-4f3a-91b4-1bd98e78c3bd	00145527-44e1-4379-bf27-1cd1334ff82f	6f0171cb-68d4-4e72-aacb-f9579174791b	ส้มโอ	120.00	1	6.00	f
1b72901b-cf85-4e63-b2a1-18292f6e7bdf	15d1ab3d-163d-4109-9d6a-77c93faf423a	fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	70.00	2	0.00	f
1d4c77fb-b481-455b-a447-10528f51c378	6e41f83c-3aa4-474f-bc6b-a3dc342c3e9d	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	2	9.00	f
1e31111f-66b4-493e-96d6-6467e6098ce9	643da9c0-b010-40a1-906d-069292244de9	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	1	0.00	f
1eddb866-15a7-4731-b1c8-20b608ae0683	d112e2a7-95a0-4452-974f-b7c9aa61bcfe	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	0.00	f
1fd6d9e9-ff89-4ca8-b046-d99f45ca3544	befbc1f2-c375-4972-8bcb-ae2ff34d3f71	6f0171cb-68d4-4e72-aacb-f9579174791b	ส้มโอ	120.00	1	6.00	f
2155793f-5812-4202-adb2-429f6832284a	0478dace-35a5-4a6e-980c-7fd38473bd2c	eecea84e-09c5-4ea5-8a73-0f30d8b9f38d	แก้วมังกร	40.00	1	2.00	f
21bb0ee1-7021-4838-b07a-f6e0278c610e	a1ca8461-7c06-419a-8f0c-3f6ec783cfb0	39800e29-c024-4057-94f9-900b83b2cfb9	ทุเรียน	350.00	3	105.00	f
223895aa-fe3c-4769-8944-1b7214def017	e359309e-a5d1-428c-8be7-0f979fc362da	7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	30.00	2	0.00	f
2348e357-1c18-4fde-b4b8-7e0ebea819d8	903034c2-bafb-475a-b12d-fa98ef171491	7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	30.00	3	0.00	f
23b9cf66-7e18-49ab-ad3b-a878241b43ec	cac3eb6f-1d49-4632-a5f8-86b90bd0ff4b	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	3	0.00	f
2475dffb-58b9-406b-9c10-2f38884958cc	c720933c-33fc-4b09-a983-24597eed5a93	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	1	0.00	f
26008028-5395-42a0-b5f0-711c629da323	af19f8df-eab7-4464-b392-1142f4b90ee9	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	1	1.00	f
26a016df-1524-410f-92d6-5956f0055c5a	13fcd4b2-40d4-4dc2-a408-8c33ddfa7c1e	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	1	0.00	f
285c57fb-e687-4c25-b62c-a5591e13ec46	d926d601-8e9a-4ee8-bbbc-57a6726c058c	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	1	1.75	f
28ada4d3-93af-461c-ad99-ea4d1fc90cee	8a1a47e5-8339-4977-b08a-94f0a8a1ee30	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	3	0.00	f
290c7195-8010-4a3d-a919-0be76fd47097	71b16cb0-b2c6-45b7-885a-79bd7d340987	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	1	1.00	f
2a033c81-d2df-4ed9-989f-2e466dbdf0d7	f442a4d5-df0b-4228-afd0-be38f995446e	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	3	10.50	f
2a2921bb-a41c-416f-8c70-2076d61a1b9d	e8788684-54c9-421c-8a29-81b1dd06884d	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	3	3.00	f
2a571819-39d3-4d1c-beef-07fa5fe1dc7b	6ea0ba80-f62e-4076-a667-70d4b8701543	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	2	2.50	f
2a6e4912-3b52-41bc-b629-99a5641eb03c	83d45815-b952-4234-9040-fefed54c44d1	90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	50.00	2	10.00	f
2ab99df4-720b-42f8-ba2e-23dd8802e199	f73e9f70-39fb-431b-81f6-2f0ffb0b194d	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	0.00	f
2c8f0596-7029-4ece-8d6f-4d4e92b38733	46e4c058-dab3-4904-b962-dd21d188ca7e	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	1	1.00	f
2d15aa8d-6a75-48cb-b980-9ff402ea216d	5b50a668-1682-4d59-8c27-12394cc83798	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	1	1.25	f
2e52619a-e326-442d-9516-0acba2e5c232	76423198-d601-4d1e-84e2-3f24e7fb41fe	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	0.00	f
2e7dc784-8e2e-443e-a808-3cf5a61f3dff	a99c0ecd-ba08-4150-93b6-361a25fe27ec	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	1	2.25	f
30713dc7-dcac-4bcb-a1d0-7dcd47cb2574	67e95751-1bf5-4229-a61f-267d4933cb6f	7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	30.00	1	1.50	f
31274782-7847-49f5-99cb-12b8b43f28df	548ffa6d-e8a1-45d3-a38b-22a90780b6cc	c2ef1b09-710a-4c4c-844e-dbb6bde791d5	มังคุด	80.00	1	8.00	f
330d6112-bfe5-4496-9231-0029b7551fe2	4af56563-e50d-4126-b1c6-fe830cd9ef90	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	3	22.50	f
3486f45e-e07a-4d44-97ab-afceeafb0c47	c8fc8054-13d1-4a73-bac8-db263083e7e3	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	1	0.00	f
352c4516-107c-4895-ba0a-da59a88e0097	4f2ef754-a0ce-421a-8879-0df3addbf31f	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	1	1.00	f
36145196-1fa6-46bb-9afe-2585d423a29b	ccc74ea3-4aaf-450b-be35-d0f44c1e3580	eecea84e-09c5-4ea5-8a73-0f30d8b9f38d	แก้วมังกร	40.00	1	4.00	f
36d2c8b0-9476-4e14-92b7-7cfe96bafa3f	1d0c79e2-504c-46a8-8837-aa95b1af3a76	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	2	9.00	f
3870cbe4-6b1e-4bc1-95a5-0ec56059255f	8597fb72-c80d-4eff-b6b7-02fa4872cd76	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	4.50	f
38967083-9817-480f-9772-d70b04ad8dbf	40037ad2-8bf2-4b60-8c23-e16348446b07	90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	50.00	2	10.00	f
39457ac5-72a7-41fa-be21-b56db7c37a22	a99eff07-5cc0-4ca5-8c62-df54072ede49	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	0.00	f
3b66a9fe-9340-4156-b5c5-8432466f891f	d926d601-8e9a-4ee8-bbbc-57a6726c058c	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	1	2.25	f
3c4c3efc-9166-4405-958e-e1bb5eee9851	d98e3180-6dda-4b15-8d26-149d210832fd	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	3	13.50	f
3c9fd443-8dc5-475c-94de-aedef26fcc5d	3aa54a1a-97d5-4481-9536-5670508ace38	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	2	9.00	f
3cffd612-88ee-4b39-96bf-9ca9f6a62713	cb865eee-8177-4bc5-ba70-10376e4d1d9c	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	3	6.75	f
3d47b31d-48c0-47bc-82ba-6944b8314bc5	29c40020-fba3-466b-a2d4-e2201d1dea76	39800e29-c024-4057-94f9-900b83b2cfb9	ทุเรียน	350.00	1	35.00	f
3dd95924-e2d8-49fc-9bfc-b05062d20526	d926d601-8e9a-4ee8-bbbc-57a6726c058c	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	1	2.25	f
3e700a23-c343-4992-9eb5-bbd05e01944a	5ff9009f-3f74-4715-b21a-4657e418d32a	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	1	3.50	f
3e9d6e84-617a-4fb4-877e-b241a982a30f	74dc0830-eae7-4ec8-8b2a-bc68c74e0103	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	1	0.00	f
3ea983d3-1997-4574-825f-3da6ac30fc54	4af56563-e50d-4126-b1c6-fe830cd9ef90	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	1	1.75	f
3f79cc87-e831-4375-9ca8-26038c4bbcf5	903034c2-bafb-475a-b12d-fa98ef171491	fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	70.00	1	0.00	f
3fab882d-4c20-478e-888e-fdb7489a898c	3179f621-d52b-4cf1-9256-52d8bdd17056	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	2	2.50	f
41f16774-14fa-4e96-a70a-3987adf1688f	0478dace-35a5-4a6e-980c-7fd38473bd2c	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	2	15.00	f
42489a55-a5bc-4984-81b0-a30eea797f5b	d98e3180-6dda-4b15-8d26-149d210832fd	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	2	0.00	f
43a746f5-0f1a-4be2-b0ac-bc5911c21cbd	00f29feb-1bf7-4e26-b09c-c1b07b019585	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	3	5.25	f
441e1b8b-1075-423f-b401-fe2607f9b3a2	15d1ab3d-163d-4109-9d6a-77c93faf423a	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	2	0.00	f
4552cd8c-d978-4aba-8d82-61d09c7286ac	67e95751-1bf5-4229-a61f-267d4933cb6f	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	1	1.75	f
45c37960-8235-41b4-97d3-7c89ef0fb658	001cfacf-bfa1-4ab5-8af3-198493eb0901	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	1	2.25	f
469812d8-8e29-4684-b7c5-b3f5b5c0ab22	d98e3180-6dda-4b15-8d26-149d210832fd	7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	30.00	3	0.00	f
4801f0b3-61af-4917-b959-6818bf1fc691	7677e7d5-b80b-4594-a783-114d4a588a31	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	1	1.50	f
489e6d94-f990-4128-b334-305f76094039	02777b26-91b2-44da-a216-237509a7b109	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	3	0.00	f
48ead42a-da90-406e-af5c-bc7981cc5faa	83d45815-b952-4234-9040-fefed54c44d1	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	2	4.00	f
4f9802b9-cfb5-4ef3-bdb4-080b1bbf7ea1	2192d342-5bbc-4b73-a595-85c51f3a11e2	fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	70.00	3	21.00	f
4fdef6cb-ff14-4441-9e3e-3459e6608a11	e314360d-680a-42a5-b55f-ce1b62735e66	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	2	7.00	f
4feebc83-68e8-41a4-92ef-6532af805536	73772ea6-fed8-49d6-8158-f93f9832803e	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	3	0.00	f
5237c0b3-766f-4166-b2e5-f4ab33c44a2a	02777b26-91b2-44da-a216-237509a7b109	a11a3213-9d2f-4ce0-809a-223692bc89e5	ลูกแพร์	30.00	1	0.00	f
52764217-4baa-4d8e-8630-4b6e783b792b	2b335421-5f2d-4abe-b9de-16b1b1a430d5	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	2	5.00	f
549af0e7-2b8d-49bc-b747-64b0b78d680f	6ea0ba80-f62e-4076-a667-70d4b8701543	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	3	6.75	f
54e62ae1-e8c0-4978-82ef-bbcac92c6dc1	00f29feb-1bf7-4e26-b09c-c1b07b019585	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	1	2.25	f
54f18d52-b52e-4d4e-bd32-48d189273f69	643da9c0-b010-40a1-906d-069292244de9	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	0.00	f
5518ceb0-ca4f-4a87-8af9-a5abd224dbcd	767109a3-be3b-459c-9035-7a7d58c631d3	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	1	4.50	f
569298d1-cd04-4176-b744-5389a7ff18fb	d739e54f-28b3-4995-b854-8dde5e8c80e7	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	1	0.00	f
57ac7ae8-4d96-4de6-9496-40cf5a84de29	5821e6f8-8b5f-4aa0-bda7-f6d548117cd3	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	3	13.50	f
581aabb7-e583-4fa2-9fc9-85b236773a9d	76423198-d601-4d1e-84e2-3f24e7fb41fe	7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	30.00	2	0.00	f
5a9dac37-73f0-4302-bb7b-0cf0e9a40ac1	a1ca8461-7c06-419a-8f0c-3f6ec783cfb0	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	2	7.00	f
61cf6ca0-a3d4-4bd2-b4d3-3fb1fa042a1a	702a5e9e-1d71-4732-bb53-2511b1fe42c3	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	2	3.00	f
6552ab88-a584-462f-87a8-9a35829a9663	74dc0830-eae7-4ec8-8b2a-bc68c74e0103	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	2	0.00	f
662ad313-e6bf-4f58-8336-8aa029baf3fc	f73e9f70-39fb-431b-81f6-2f0ffb0b194d	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	1	0.00	f
6654e289-5736-4418-be40-5f7d44ec729f	2192d342-5bbc-4b73-a595-85c51f3a11e2	68bd5af1-2587-48a6-9592-788577cde3fb	ลิ้นจี่	90.00	3	27.00	f
66f7231a-feb3-42e2-b728-775ece529ec6	9a71d398-6b79-431d-912d-2e7e80a91364	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	3	0.00	f
69821815-1785-46e6-8a16-83f5275185e1	7808a41a-2a32-420a-ae25-7a8a3acb394f	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	2	12.00	f
6982a8b3-7a9d-466d-a7d9-b12851d7d1c7	5ff9009f-3f74-4715-b21a-4657e418d32a	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	2	3.00	f
6aab8ec1-2d94-4f9b-ad35-5e5e04cead4a	40037ad2-8bf2-4b60-8c23-e16348446b07	6f0171cb-68d4-4e72-aacb-f9579174791b	ส้มโอ	120.00	2	24.00	f
6c7e167e-4619-41a2-bd78-db493739d7d8	f99b3e90-deae-474e-961d-826d7b100fcf	c2ef1b09-710a-4c4c-844e-dbb6bde791d5	มังคุด	80.00	1	4.00	f
70345bfd-a468-488e-9d41-03bd7f7feba8	646501dc-f1b3-4c7d-8f19-16bc4393da68	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	1	0.75	f
71787040-ba96-46e8-b95f-24a5e7986373	702a5e9e-1d71-4732-bb53-2511b1fe42c3	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	2	9.00	f
7272bd90-fb58-423d-9598-a8609e24b760	23fa493e-2291-4079-b056-7cc6777bd96a	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	3	7.50	f
73bb3ac8-2b61-4c11-a873-15767e26c930	ccc74ea3-4aaf-450b-be35-d0f44c1e3580	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	1	3.50	f
75dfc707-8b18-446d-b5e6-16216ec96a74	01105c32-dabf-4826-8091-02f50f7a2e6c	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	2	0.00	f
77503c7e-e8dc-4e33-a7e3-f9ebded74a23	6723fc8b-aba8-4686-888e-35aa339d0100	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	3	6.75	f
77f2c848-20a8-496d-870e-52917e684ce5	eac6d959-573d-49ef-ac29-284270011fcf	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	1	15.00	f
78ba4826-9081-49fb-88ab-c8ce85331176	e8788684-54c9-421c-8a29-81b1dd06884d	fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	70.00	1	3.50	f
79acc1d6-8bc5-4145-8aa6-92561dd57772	4aeafb8d-157b-435e-9010-85929715c037	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	2.25	f
7a9bd8f4-abee-4af1-9eac-d8f9069a722d	1afa56d1-3302-4444-92cf-9d5880fba940	fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	70.00	2	7.00	f
7aa6b262-c9f6-4a3c-8731-d152af3c0483	5ff9009f-3f74-4715-b21a-4657e418d32a	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	1	15.00	f
7b1ea5f5-8a2b-4b72-b890-6ca88ec1a07c	d112e2a7-95a0-4452-974f-b7c9aa61bcfe	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	1	0.00	f
7b2c574d-8867-432a-84cd-c449ef5fba50	e4cb1229-e62f-4df6-9b7b-5e19ea0999c5	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	1	3.50	f
7cae75d0-bdca-47d7-9e18-c9e40286af3c	5b50a668-1682-4d59-8c27-12394cc83798	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	2.25	f
7dc2e8fc-add2-4259-8365-f492ed55d267	e6299fcc-d9f2-49ea-8d03-8544c8893a3e	6f0171cb-68d4-4e72-aacb-f9579174791b	ส้มโอ	120.00	1	0.00	f
7dd4ff39-f976-4427-bad4-b7b923afceab	befbc1f2-c375-4972-8bcb-ae2ff34d3f71	7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	30.00	1	1.50	f
7e648482-3245-4e77-bb71-a861d0887631	2f6ace7a-ee1c-49ae-96c8-806f56099c03	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	2.25	f
81c3b7b2-b779-4415-832c-54f2c80e8eb0	7808a41a-2a32-420a-ae25-7a8a3acb394f	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	1	4.45	f
81f129a0-a789-464b-a149-57288510bd9b	beaf84c3-bf8b-4bce-930b-c821fa73936b	eecea84e-09c5-4ea5-8a73-0f30d8b9f38d	แก้วมังกร	40.00	2	4.00	f
86123ec3-2e58-4ce1-8582-cd3decd710f9	767109a3-be3b-459c-9035-7a7d58c631d3	90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	50.00	3	15.00	f
8744a0fb-5a2c-47d5-8a6a-a9fb567bea62	eac6d959-573d-49ef-ac29-284270011fcf	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	2	3.00	f
878c0dd1-0254-456d-be16-aff75ffce3f3	dec33495-bbae-4eac-a2be-5d9425358a7b	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	1	0.00	f
87f8b785-54c6-4d00-a49a-c3950dfe4049	23fa493e-2291-4079-b056-7cc6777bd96a	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	4.50	f
88b3e880-434f-4aed-918f-b58d39957b13	6cd4f5cf-fd99-4a0d-b00e-cd1b5f2b4fcc	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	1	2.50	f
89e76e9c-c589-424c-bc34-940932fc3712	cbfa1ae4-7b8b-4b01-9a40-ca33984d8590	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	3	0.00	f
8a371a1e-8776-41f9-a419-afbed775bdc0	98e805ca-e04a-46c2-89fd-0a9cf49863bb	a11a3213-9d2f-4ce0-809a-223692bc89e5	ลูกแพร์	30.00	2	0.00	f
8cc5a98e-59cf-4dbb-b07a-895d3fa9641f	7ddf0a8d-d426-46f3-94b0-f5f18a03870f	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	1	0.00	f
8e50eaad-a415-4d82-a210-6a3059bcbe69	00f29feb-1bf7-4e26-b09c-c1b07b019585	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	1	0.75	f
9197000a-80dc-4cdf-b9e5-211adbd91912	ea1d73f4-9e00-4ec3-874c-ff80808e388d	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	2	0.00	f
92233934-4d47-4ffe-8ba5-8ffb2965eb29	c720933c-33fc-4b09-a983-24597eed5a93	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	2	0.00	f
94a4ce6e-41a0-42c7-833f-8e5ca1a5b43b	00f29feb-1bf7-4e26-b09c-c1b07b019585	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	3	22.50	f
954e4064-dead-493d-a832-39d76d2017ac	6ea0ba80-f62e-4076-a667-70d4b8701543	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	3	5.25	f
9772d852-bf3d-44b5-be83-37dff168549c	6cd4f5cf-fd99-4a0d-b00e-cd1b5f2b4fcc	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	3	10.50	f
977761b6-aa33-48ed-aa07-644269899c62	1b1a22f8-80e0-4e37-a381-aa7bf00b573f	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	2	2.50	f
97d61973-0ed7-43ef-8256-42342d70e343	cbfa1ae4-7b8b-4b01-9a40-ca33984d8590	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	1	0.00	f
995364f5-aca9-4ad8-a107-cd4f652ae1b4	903034c2-bafb-475a-b12d-fa98ef171491	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	3	0.00	f
9b0ebc83-f3c3-4195-b014-ad78c55a7506	01105c32-dabf-4826-8091-02f50f7a2e6c	eecea84e-09c5-4ea5-8a73-0f30d8b9f38d	แก้วมังกร	40.00	1	0.00	f
9b46348f-708b-48fb-9b93-827de23cdbfe	acca3745-12c7-4a11-9403-793d553028bb	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	2.25	f
9c1312b8-457b-4aaf-a738-f887bab1e160	f73e9f70-39fb-431b-81f6-2f0ffb0b194d	68bd5af1-2587-48a6-9592-788577cde3fb	ลิ้นจี่	90.00	2	0.00	f
9e69d66f-db2a-44f2-9501-7002be911854	eed80978-92d1-432f-a744-6e7ee7e0878a	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	3	0.00	f
a01044dc-27f0-444d-8302-381c3f1646e4	956a0e20-fed7-49fd-8a59-be67fb614a4c	fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	70.00	1	3.50	f
a0daecb5-ddb4-4227-b380-be1a4a153dd6	7677e7d5-b80b-4594-a783-114d4a588a31	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	3	36.00	f
a167ddcc-db64-460c-8995-2530d4d51e2c	f99b3e90-deae-474e-961d-826d7b100fcf	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	1	1.25	f
a26edde5-b685-4d97-9376-ab0a1f7e4871	00145527-44e1-4379-bf27-1cd1334ff82f	7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	30.00	2	3.00	f
a33204e9-37f4-4eb9-b18e-8899f8076b45	3dae9c73-0462-487c-aa57-c6fe9ac3ec98	c2ef1b09-710a-4c4c-844e-dbb6bde791d5	มังคุด	80.00	3	24.00	f
a41178f8-61f7-4045-a94f-139ce23b2d54	c8fc8054-13d1-4a73-bac8-db263083e7e3	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	1	0.00	f
a5539429-43e8-4e55-99ab-8f4421840950	2192d342-5bbc-4b73-a595-85c51f3a11e2	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	2	3.00	f
a65a78b2-d1d8-45e0-bc9d-78db5cd54c52	3c8a6fda-1fde-4ff1-a2d3-ed134067c298	a11a3213-9d2f-4ce0-809a-223692bc89e5	ลูกแพร์	30.00	1	1.50	f
a6ac6814-1d3f-45e3-9266-d712a44d99f7	6723fc8b-aba8-4686-888e-35aa339d0100	90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	50.00	3	7.50	f
a6d26590-2bf3-40ea-8642-2b74e7559f0c	1b1a22f8-80e0-4e37-a381-aa7bf00b573f	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	2	2.00	f
a86c00c0-a6c4-458f-9628-4d8f67af295c	3179f621-d52b-4cf1-9256-52d8bdd17056	39800e29-c024-4057-94f9-900b83b2cfb9	ทุเรียน	350.00	2	175.00	f
aa064324-4e43-456a-a656-fa70377424ac	eac6d959-573d-49ef-ac29-284270011fcf	68bd5af1-2587-48a6-9592-788577cde3fb	ลิ้นจี่	90.00	3	27.00	f
aa6f0710-7412-4c5f-9fd3-c337e305e5a8	96541fc4-6bf2-42d7-a10a-73cbaf1379aa	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	1	0.00	f
ab04edb5-baae-4807-9031-6b84f352179c	2f6ace7a-ee1c-49ae-96c8-806f56099c03	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	1	1.00	f
acd9eeff-b4bd-44f8-a081-ada99288f781	1b1a22f8-80e0-4e37-a381-aa7bf00b573f	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	1	0.75	f
ad6f6e03-25f3-45eb-a797-aa5308d2604c	71b16cb0-b2c6-45b7-885a-79bd7d340987	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	1	1.75	f
ad8b69c2-9e9f-4ff5-8da0-0490fb6b85b4	cac3eb6f-1d49-4632-a5f8-86b90bd0ff4b	39800e29-c024-4057-94f9-900b83b2cfb9	ทุเรียน	350.00	1	70.00	f
ae2cf20b-1525-4cb1-b354-57e4728a2787	cbfa1ae4-7b8b-4b01-9a40-ca33984d8590	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	1	0.00	f
b158e0dc-9fe5-43b5-b52e-821e1310e388	001cfacf-bfa1-4ab5-8af3-198493eb0901	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	3	6.75	f
b184e6c4-c5b6-42d4-a8de-afd68fb3d283	e359309e-a5d1-428c-8be7-0f979fc362da	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	2	9.00	f
b2bef4f0-d980-4a7a-b89c-0cd931073b36	368f822d-6e20-445b-8820-afa77bfe163b	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	2	9.00	f
b49f530a-741d-4e91-b727-c9c53b96b5bf	e4cb1229-e62f-4df6-9b7b-5e19ea0999c5	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	3	13.50	f
b6197014-24a7-4616-8d6f-a6a6c1c48376	8625ee6c-5f2b-41cd-979f-60960fee6dc6	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	2	7.00	f
b693073f-acc2-4719-9454-d2f218cad34e	d926d601-8e9a-4ee8-bbbc-57a6726c058c	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	1	1.00	f
b720fdee-dd83-4818-a1bf-c963d531de78	b0908038-6495-48d3-9e3f-21863d908697	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	3	26.70	f
b92a90fd-0af0-4b5e-8df0-9208ef2e2c00	3dae9c73-0462-487c-aa57-c6fe9ac3ec98	eecea84e-09c5-4ea5-8a73-0f30d8b9f38d	แก้วมังกร	40.00	1	4.00	f
b9674da5-542f-4cfe-8a86-44cf1ab86459	646501dc-f1b3-4c7d-8f19-16bc4393da68	90fb3f3a-ad44-4ebb-9bbd-ea0acfc96441	เงาะ	50.00	3	7.50	f
ba107b35-33ac-4217-8b68-7f6099a7d20f	b0908038-6495-48d3-9e3f-21863d908697	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	2	7.00	f
bbd84cd6-2e78-4d1d-8c91-6c7e2c44bc3a	13fcd4b2-40d4-4dc2-a408-8c33ddfa7c1e	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	3	0.00	f
bf0364d1-4e59-4f23-b96b-2da4035b419f	40037ad2-8bf2-4b60-8c23-e16348446b07	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	2	30.00	f
bfa7b02b-c59d-4f81-b7f8-f40a120298f1	ea1d73f4-9e00-4ec3-874c-ff80808e388d	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	2	0.00	f
c1c897a0-912a-4156-be0a-110a1e068657	5821e6f8-8b5f-4aa0-bda7-f6d548117cd3	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	1	12.00	f
c2ca26ea-d8e4-4e14-8d91-37e03f748589	c720933c-33fc-4b09-a983-24597eed5a93	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	0.00	f
c3ce42fb-0e5b-4189-87da-dde260f21b44	4aeafb8d-157b-435e-9010-85929715c037	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	3	6.75	f
c4c50b42-fc4d-41aa-997a-d5005ad1f257	64d90118-8721-41a6-b7ae-5be729f0d1d6	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	2	0.00	f
c5558a98-eea1-4104-aa27-707144ed524e	64d90118-8721-41a6-b7ae-5be729f0d1d6	fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	70.00	2	0.00	f
c5ec929e-a348-4c7c-8ad3-448c28e7b287	1d0c79e2-504c-46a8-8837-aa95b1af3a76	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	1	4.50	f
c6ce940e-6ab6-4295-8a0e-957f5968514b	13fcd4b2-40d4-4dc2-a408-8c33ddfa7c1e	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	3	0.00	f
c72c357e-9fe6-4e93-bd34-5d196239b173	1107e2d9-d47b-40fe-9552-3e2c63ce39f5	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	3	7.50	f
c862ffbc-7987-47d4-9df4-ff62cf1f57ff	5ff9009f-3f74-4715-b21a-4657e418d32a	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	2	24.00	f
c9697600-da37-4112-8271-80bab6d7bf77	6cd4f5cf-fd99-4a0d-b00e-cd1b5f2b4fcc	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	1	12.00	f
ca98de62-2979-4cf0-be6b-8b842ea60b1f	001cfacf-bfa1-4ab5-8af3-198493eb0901	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	3	5.25	f
cae19cc5-dfbb-4668-98d5-f90f9f7f8dbf	3aa54a1a-97d5-4481-9536-5670508ace38	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	4.50	f
cbe9ad71-9e97-4230-b190-0d009ddd02b5	e8788684-54c9-421c-8a29-81b1dd06884d	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	3	2.25	f
ccce1759-318d-469b-a839-15965257e364	15d1ab3d-163d-4109-9d6a-77c93faf423a	c2ef1b09-710a-4c4c-844e-dbb6bde791d5	มังคุด	80.00	2	0.00	f
ccee4544-b16a-4936-a310-7b1ee71aeb4d	1b1a22f8-80e0-4e37-a381-aa7bf00b573f	fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	70.00	3	10.50	f
cd1270c9-2349-4423-a562-39d601532897	71b16cb0-b2c6-45b7-885a-79bd7d340987	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	2	4.50	f
ce064ad0-a1f9-473d-b575-a2f838f7c378	d98e3180-6dda-4b15-8d26-149d210832fd	a11a3213-9d2f-4ce0-809a-223692bc89e5	ลูกแพร์	30.00	2	0.00	f
ce56b1a9-3e61-4d72-b0b7-fddab4914987	d739e54f-28b3-4995-b854-8dde5e8c80e7	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	3	0.00	f
ceee7aa0-223e-4619-82a7-19d71fb8cee2	f99b3e90-deae-474e-961d-826d7b100fcf	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	2	4.50	f
cf82314b-039a-4977-95eb-6be54e9e8904	2f6ace7a-ee1c-49ae-96c8-806f56099c03	6f0171cb-68d4-4e72-aacb-f9579174791b	ส้มโอ	120.00	1	6.00	f
cf8c8d9a-b079-4356-9c2f-171d456424d4	f99b3e90-deae-474e-961d-826d7b100fcf	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	2	12.00	f
d10a38d8-59e6-48ee-ad01-3b3d8e62897a	6ea0ba80-f62e-4076-a667-70d4b8701543	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	2	1.50	f
d22af01f-7cdf-4b4b-8fed-f9c59a819254	c720933c-33fc-4b09-a983-24597eed5a93	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	3	0.00	f
d3c634d9-4880-4957-aec5-c965f7c46861	e4cb1229-e62f-4df6-9b7b-5e19ea0999c5	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	3	26.70	f
d46c5d87-388a-41e4-b096-9e31c031253a	74dc0830-eae7-4ec8-8b2a-bc68c74e0103	7c7b809f-b769-451d-a364-e8142469f95b	มะละกอ	30.00	1	0.00	f
d687ecf7-e780-421f-b54b-b68ed1b2d10f	e359309e-a5d1-428c-8be7-0f979fc362da	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	1	0.00	f
d70587ef-6c34-41ea-8fe7-8f24b29b46ce	9a71d398-6b79-431d-912d-2e7e80a91364	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	3	13.50	f
d86a7d84-14c5-49fc-a006-1262becc047e	49def2db-73a8-4cd9-9986-227bf18e2137	39800e29-c024-4057-94f9-900b83b2cfb9	ทุเรียน	350.00	1	35.00	f
d887a56e-78e3-4c64-ab2b-df095fe8110f	57a2bda9-8dfc-4ae8-af4e-4c45298b4083	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	1	2.25	f
db0fd238-83d6-457b-a59e-b43846f6d4c5	ea1d73f4-9e00-4ec3-874c-ff80808e388d	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	2	0.00	f
dc064011-b4c5-457b-8986-311831ce942b	beaf84c3-bf8b-4bce-930b-c821fa73936b	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	2	4.50	f
dc0afea7-cb57-4870-acb8-769b4e520be2	83d45815-b952-4234-9040-fefed54c44d1	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	2	30.00	f
dc99d03d-98c3-481b-a044-b59a955e9678	5821e6f8-8b5f-4aa0-bda7-f6d548117cd3	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	1	4.50	f
de80ca1c-518c-4c14-8bb2-4bdeb96c938e	74dc0830-eae7-4ec8-8b2a-bc68c74e0103	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	1	0.00	f
e155127d-68a0-45dd-b644-4c625929a228	368f822d-6e20-445b-8820-afa77bfe163b	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	2	4.00	f
e17bcd94-2733-4c9b-9e7b-3265df815453	befbc1f2-c375-4972-8bcb-ae2ff34d3f71	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	2	2.50	f
e3804664-fc65-45c3-a9be-e161a9fba8e7	e6299fcc-d9f2-49ea-8d03-8544c8893a3e	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	2	0.00	f
e3862a1a-9862-4eeb-bcee-0290b8190ef2	dec33495-bbae-4eac-a2be-5d9425358a7b	fdd30cff-bc77-47bb-aa83-ddee16401494	ลำไย	70.00	2	0.00	f
e4512eb1-1428-4b4e-ba76-0b1459fe6e5b	6e41f83c-3aa4-474f-bc6b-a3dc342c3e9d	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	3	10.50	f
e4cb21ef-f898-49de-b494-9cd0c6ff10f7	06443c92-76c1-4894-83f7-7d1f7bf16c05	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	2	44.50	f
e4eb77a0-cff9-426e-b4b6-2e94e2607a6c	1107e2d9-d47b-40fe-9552-3e2c63ce39f5	360a3c0d-6130-43f4-b53e-92e775636ed0	สับปะรด	35.00	2	7.00	f
e6a1bf07-1254-48e9-8704-41edf6aed63f	71b16cb0-b2c6-45b7-885a-79bd7d340987	68bd5af1-2587-48a6-9592-788577cde3fb	ลิ้นจี่	90.00	1	4.50	f
e6d90c97-3e6f-4394-9531-067f5bcacbd8	0478dace-35a5-4a6e-980c-7fd38473bd2c	35fa5757-2987-4814-93c0-3aa0d141b7ca	มะพร้าว	35.00	2	3.50	f
e7214673-e480-4b07-be96-997e3c426c05	6723fc8b-aba8-4686-888e-35aa339d0100	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	2	15.00	f
e8fa763f-e9f9-4d61-8449-1b1edcd19c22	3aa54a1a-97d5-4481-9536-5670508ace38	39800e29-c024-4057-94f9-900b83b2cfb9	ทุเรียน	350.00	2	70.00	f
e9a30905-0803-4659-83ae-c78fee4468bc	ea1d73f4-9e00-4ec3-874c-ff80808e388d	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	1	0.00	f
ea0fa15e-1351-4e08-8b72-a23047938df0	f442a4d5-df0b-4228-afd0-be38f995446e	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	2	3.00	f
eadf282a-f250-408d-a4e4-70cb0bac608a	7ddf0a8d-d426-46f3-94b0-f5f18a03870f	c2ef1b09-710a-4c4c-844e-dbb6bde791d5	มังคุด	80.00	1	0.00	f
ebf10eef-b1f0-494c-b7af-9c1f9ad0fbb1	3179f621-d52b-4cf1-9256-52d8bdd17056	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	1	6.00	f
ee0479c6-fa4b-4767-b315-09ba94d51a56	25d773b1-833e-46ad-8b78-404b664101a6	e8f4c8da-5a87-455e-b4dc-f3fe5200b963	แอปเปิล	25.00	3	3.75	f
ef000d7e-f792-491c-b152-7d82b949cb61	00145527-44e1-4379-bf27-1cd1334ff82f	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	3	3.00	f
f04ff40b-5376-4cf1-b85e-91c31cad79e3	767109a3-be3b-459c-9035-7a7d58c631d3	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	1	4.50	f
f20f07d7-5bbd-43cd-b783-824f94038b0e	a1ca8461-7c06-419a-8f0c-3f6ec783cfb0	fe17a147-9367-454e-b21c-0b49d9c44def	องุ่น	120.00	3	36.00	f
f22e3dcd-423b-49ef-8476-228efa0bc45f	64d90118-8721-41a6-b7ae-5be729f0d1d6	c2ef1b09-710a-4c4c-844e-dbb6bde791d5	มังคุด	80.00	3	0.00	f
f2c11d74-9d13-4522-a444-476791f72736	702a5e9e-1d71-4732-bb53-2511b1fe42c3	8fd0fd64-d87a-4d1d-a4f5-ac11d63a9ed4	กล้วยหอม	45.00	2	9.00	f
f4abb975-505c-4424-bf54-bff25e2f56d5	d112e2a7-95a0-4452-974f-b7c9aa61bcfe	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	1	0.00	f
f6c89535-d5e5-4513-bd09-e1b3412a6555	7808a41a-2a32-420a-ae25-7a8a3acb394f	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	2	2.00	f
f7a795c8-4ea9-46bd-b1c2-25c0a745be8d	5b50a668-1682-4d59-8c27-12394cc83798	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	3	13.35	f
f8ab7a73-13b7-41c9-838e-3e4051a0e379	a99c0ecd-ba08-4150-93b6-361a25fe27ec	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	1	1.00	f
f903b1e7-96ca-41fe-a0da-1966d3515558	f442a4d5-df0b-4228-afd0-be38f995446e	bb3598f2-0cb1-4f30-b22f-9282b23d9ab9	ฝรั่ง	20.00	1	2.00	f
fadcac5c-da9e-4145-bc4f-06b7edc4ad27	646501dc-f1b3-4c7d-8f19-16bc4393da68	e1a8f3f5-cf72-4701-88cc-0635a07b876b	แตงโม	89.00	2	8.90	f
fd50f932-ccbc-45fe-a7e5-e341693fd116	af19f8df-eab7-4464-b392-1142f4b90ee9	0bc9d835-f141-4624-b425-824f256dc6e0	ส้ม	15.00	2	1.50	f
fd74a2c6-0878-40dc-9eaf-15d16eb7488b	f36146da-0f9f-4530-a2d4-e2baeff33613	1a403c33-7de2-4e69-b7d3-0abdff6379e1	กีวี	20.00	2	4.00	f
fde65b07-baab-4753-843b-8108ffcc02bc	15d1ab3d-163d-4109-9d6a-77c93faf423a	8216c6a6-ae6b-4f3a-8618-3d0bc7a2e5af	สตรอว์เบอร์รี	150.00	2	0.00	f
fe36b30b-4e8a-4c68-a439-0f786282d60e	76423198-d601-4d1e-84e2-3f24e7fb41fe	4036fb39-a055-4bf7-b14b-3890d5086085	มะม่วง	45.00	2	0.00	f
fe41e75c-9fd2-4da9-aca7-c39bdaefa360	a99c0ecd-ba08-4150-93b6-361a25fe27ec	c2ef1b09-710a-4c4c-844e-dbb6bde791d5	มังคุด	80.00	1	4.00	f
\.


--
-- Data for Name: sales; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.sales ("Id", "StaffId", "MemberId", "CreatedAtUtc") FROM stdin;
00145527-44e1-4379-bf27-1cd1334ff82f	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-13 10:50:13.15559+00
001cfacf-bfa1-4ab5-8af3-198493eb0901	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-09 06:51:17.942732+00
00f29feb-1bf7-4e26-b09c-c1b07b019585	ffc495b4-225f-41a2-b24c-82844625c0d6	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-08-10 09:43:14.760627+00
01105c32-dabf-4826-8091-02f50f7a2e6c	c0a0d489-a810-408c-95de-5b3380e49b83	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-08-06 04:50:48.72831+00
02777b26-91b2-44da-a216-237509a7b109	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-09-09 04:52:08.653364+00
0478dace-35a5-4a6e-980c-7fd38473bd2c	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-19 11:21:16.002014+00
06443c92-76c1-4894-83f7-7d1f7bf16c05	ffc495b4-225f-41a2-b24c-82844625c0d6	47b0ab78-bd86-4bd4-a734-db45272c8133	2026-09-14 01:13:20.232936+00
1107e2d9-d47b-40fe-9552-3e2c63ce39f5	c0a0d489-a810-408c-95de-5b3380e49b83	47b0ab78-bd86-4bd4-a734-db45272c8133	2026-09-08 03:43:49.197464+00
13fcd4b2-40d4-4dc2-a408-8c33ddfa7c1e	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-05 02:34:00.648365+00
15d1ab3d-163d-4109-9d6a-77c93faf423a	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-28 04:42:43.760511+00
1a4a454d-a96e-4247-a23b-130ec1b7a322	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-20 08:46:33.987339+00
1afa56d1-3302-4444-92cf-9d5880fba940	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-17 09:54:39.298535+00
1b1a22f8-80e0-4e37-a381-aa7bf00b573f	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-25 06:24:22.443387+00
1d0c79e2-504c-46a8-8837-aa95b1af3a76	ffc495b4-225f-41a2-b24c-82844625c0d6	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-08-20 01:49:02.195247+00
2192d342-5bbc-4b73-a595-85c51f3a11e2	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-09-12 11:09:56.893688+00
23fa493e-2291-4079-b056-7cc6777bd96a	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-08-18 10:11:05.784877+00
25d773b1-833e-46ad-8b78-404b664101a6	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-15 07:06:40.420268+00
28101201-af86-4dab-9ee6-af9b0e9c5d1d	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-09-11 03:05:19.773639+00
29c40020-fba3-466b-a2d4-e2201d1dea76	c0a0d489-a810-408c-95de-5b3380e49b83	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-09-07 06:50:53.66286+00
2b335421-5f2d-4abe-b9de-16b1b1a430d5	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-09-06 02:16:52.239499+00
2f6ace7a-ee1c-49ae-96c8-806f56099c03	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-21 07:17:56.992479+00
3179f621-d52b-4cf1-9256-52d8bdd17056	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-16 06:12:59.973482+00
368f822d-6e20-445b-8820-afa77bfe163b	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-08-17 02:27:38.616943+00
3aa54a1a-97d5-4481-9536-5670508ace38	c0a0d489-a810-408c-95de-5b3380e49b83	47b0ab78-bd86-4bd4-a734-db45272c8133	2026-08-28 09:43:16.422939+00
3c8a6fda-1fde-4ff1-a2d3-ed134067c298	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-11 04:32:01.81525+00
3dae9c73-0462-487c-aa57-c6fe9ac3ec98	ffc495b4-225f-41a2-b24c-82844625c0d6	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-09-05 10:38:22.979437+00
40037ad2-8bf2-4b60-8c23-e16348446b07	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-08-31 02:21:33.352297+00
46e4c058-dab3-4904-b962-dd21d188ca7e	ffc495b4-225f-41a2-b24c-82844625c0d6	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-08-14 10:18:27.555082+00
49def2db-73a8-4cd9-9986-227bf18e2137	c0a0d489-a810-408c-95de-5b3380e49b83	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-09-07 05:33:07.934594+00
4aeafb8d-157b-435e-9010-85929715c037	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-12 11:22:47.12397+00
4af56563-e50d-4126-b1c6-fe830cd9ef90	c0a0d489-a810-408c-95de-5b3380e49b83	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-08-12 10:32:46.660964+00
4f2ef754-a0ce-421a-8879-0df3addbf31f	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-13 06:25:12.899118+00
548ffa6d-e8a1-45d3-a38b-22a90780b6cc	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-08-25 06:00:15.485494+00
57a2bda9-8dfc-4ae8-af4e-4c45298b4083	c0a0d489-a810-408c-95de-5b3380e49b83	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-08-13 05:47:02.461209+00
5821e6f8-8b5f-4aa0-bda7-f6d548117cd3	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-08-21 06:21:03.713103+00
5b50a668-1682-4d59-8c27-12394cc83798	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-07 02:30:30.582938+00
5ff9009f-3f74-4715-b21a-4657e418d32a	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-09-03 12:01:02.522303+00
643da9c0-b010-40a1-906d-069292244de9	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-09-13 02:20:33.196116+00
646501dc-f1b3-4c7d-8f19-16bc4393da68	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-08 10:42:57.815404+00
64d90118-8721-41a6-b7ae-5be729f0d1d6	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-09-06 07:54:25.554638+00
6723fc8b-aba8-4686-888e-35aa339d0100	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-17 10:11:01.821853+00
67e95751-1bf5-4229-a61f-267d4933cb6f	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-08-08 04:25:07.202572+00
6cd4f5cf-fd99-4a0d-b00e-cd1b5f2b4fcc	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-09-04 12:53:17.232822+00
6e41f83c-3aa4-474f-bc6b-a3dc342c3e9d	c0a0d489-a810-408c-95de-5b3380e49b83	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-08-26 06:39:41.24458+00
6ea0ba80-f62e-4076-a667-70d4b8701543	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-20 01:45:05.754365+00
702a5e9e-1d71-4732-bb53-2511b1fe42c3	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-08-25 04:29:13.141975+00
71b16cb0-b2c6-45b7-885a-79bd7d340987	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-08-14 06:09:44.095646+00
73772ea6-fed8-49d6-8158-f93f9832803e	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-09-12 06:40:07.380809+00
74dc0830-eae7-4ec8-8b2a-bc68c74e0103	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-30 10:01:05.402557+00
76423198-d601-4d1e-84e2-3f24e7fb41fe	ffc495b4-225f-41a2-b24c-82844625c0d6	47b0ab78-bd86-4bd4-a734-db45272c8133	2026-08-03 09:11:09.309372+00
767109a3-be3b-459c-9035-7a7d58c631d3	c0a0d489-a810-408c-95de-5b3380e49b83	47b0ab78-bd86-4bd4-a734-db45272c8133	2026-08-23 11:09:59.516043+00
7677e7d5-b80b-4594-a783-114d4a588a31	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-09-16 04:26:54.702747+00
7808a41a-2a32-420a-ae25-7a8a3acb394f	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-14 02:08:23.762937+00
7ddf0a8d-d426-46f3-94b0-f5f18a03870f	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-02 01:40:19.655353+00
81013683-550f-4048-8167-8af278ab15d1	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-24 06:58:23.17219+00
83d45815-b952-4234-9040-fefed54c44d1	c0a0d489-a810-408c-95de-5b3380e49b83	47b0ab78-bd86-4bd4-a734-db45272c8133	2026-09-09 03:32:15.297285+00
8597fb72-c80d-4eff-b6b7-02fa4872cd76	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-09-10 10:12:06.854451+00
8625ee6c-5f2b-41cd-979f-60960fee6dc6	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-09-06 03:49:45.672598+00
8a1a47e5-8339-4977-b08a-94f0a8a1ee30	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-28 11:09:55.244511+00
903034c2-bafb-475a-b12d-fa98ef171491	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-09-16 04:50:40.813927+00
956a0e20-fed7-49fd-8a59-be67fb614a4c	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-08-12 04:14:29.962878+00
96541fc4-6bf2-42d7-a10a-73cbaf1379aa	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-03 07:00:24.332467+00
98e805ca-e04a-46c2-89fd-0a9cf49863bb	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-09-04 08:07:00.323166+00
9a71d398-6b79-431d-912d-2e7e80a91364	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-29 11:35:02.801231+00
a1ca8461-7c06-419a-8f0c-3f6ec783cfb0	ffc495b4-225f-41a2-b24c-82844625c0d6	47b0ab78-bd86-4bd4-a734-db45272c8133	2026-08-29 05:02:46.587844+00
a99c0ecd-ba08-4150-93b6-361a25fe27ec	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-22 01:16:47.242555+00
a99eff07-5cc0-4ca5-8c62-df54072ede49	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-03 04:13:39.353477+00
acca3745-12c7-4a11-9403-793d553028bb	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-08-09 02:06:07.354745+00
af19f8df-eab7-4464-b392-1142f4b90ee9	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-07 05:34:51.073363+00
b0908038-6495-48d3-9e3f-21863d908697	ffc495b4-225f-41a2-b24c-82844625c0d6	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-08-31 10:54:34.770586+00
beaf84c3-bf8b-4bce-930b-c821fa73936b	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-08-09 03:23:17.333496+00
befbc1f2-c375-4972-8bcb-ae2ff34d3f71	ffc495b4-225f-41a2-b24c-82844625c0d6	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-08-16 05:16:30.1424+00
c720933c-33fc-4b09-a983-24597eed5a93	c0a0d489-a810-408c-95de-5b3380e49b83	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-08-02 11:48:38.786305+00
c8fc8054-13d1-4a73-bac8-db263083e7e3	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-09-15 12:12:56.351645+00
cac3eb6f-1d49-4632-a5f8-86b90bd0ff4b	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-08-04 10:28:38.289086+00
cb865eee-8177-4bc5-ba70-10376e4d1d9c	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-11 04:31:54.397628+00
cbfa1ae4-7b8b-4b01-9a40-ca33984d8590	c0a0d489-a810-408c-95de-5b3380e49b83	47b0ab78-bd86-4bd4-a734-db45272c8133	2026-08-06 01:04:46.535581+00
ccc74ea3-4aaf-450b-be35-d0f44c1e3580	ffc495b4-225f-41a2-b24c-82844625c0d6	f4f1c31b-70e0-47eb-9a83-18cdbce54f63	2026-08-26 06:21:15.086427+00
d112e2a7-95a0-4452-974f-b7c9aa61bcfe	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-09-05 08:22:53.607193+00
d739e54f-28b3-4995-b854-8dde5e8c80e7	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-09-16 04:59:14.576632+00
d926d601-8e9a-4ee8-bbbc-57a6726c058c	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-15 06:24:24.723198+00
d98e3180-6dda-4b15-8d26-149d210832fd	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-09-12 02:35:08.721019+00
dec33495-bbae-4eac-a2be-5d9425358a7b	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-09-03 12:49:35.207955+00
e314360d-680a-42a5-b55f-ce1b62735e66	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-09-16 03:08:02.617978+00
e359309e-a5d1-428c-8be7-0f979fc362da	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-09-15 05:57:24.999695+00
e4cb1229-e62f-4df6-9b7b-5e19ea0999c5	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-08-26 08:26:45.134101+00
e6299fcc-d9f2-49ea-8d03-8544c8893a3e	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-09-05 04:47:29.316577+00
e8788684-54c9-421c-8a29-81b1dd06884d	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-16 04:58:42.440084+00
ea1d73f4-9e00-4ec3-874c-ff80808e388d	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-08-27 02:42:19.19551+00
eac6d959-573d-49ef-ac29-284270011fcf	ffc495b4-225f-41a2-b24c-82844625c0d6	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-09-01 09:15:12.089688+00
eed80978-92d1-432f-a744-6e7ee7e0878a	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-09-11 12:41:20.784243+00
f36146da-0f9f-4530-a2d4-e2baeff33613	c0a0d489-a810-408c-95de-5b3380e49b83	0edc01bb-e624-4ecc-9f72-16dc5bc5c0b1	2026-08-23 06:04:59.695157+00
f442a4d5-df0b-4228-afd0-be38f995446e	c0a0d489-a810-408c-95de-5b3380e49b83	ede2e7fc-31ef-403e-8f4b-02484e2d08bd	2026-09-04 07:22:19.703848+00
f73e9f70-39fb-431b-81f6-2f0ffb0b194d	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-09-02 09:28:32.33395+00
f7ba1cf1-8a25-4f71-b9c3-aa1a51c148f6	ffc495b4-225f-41a2-b24c-82844625c0d6	\N	2026-09-11 11:12:01.272554+00
f99b3e90-deae-474e-961d-826d7b100fcf	c0a0d489-a810-408c-95de-5b3380e49b83	\N	2026-08-22 11:11:39.783816+00
\.


--
-- Data for Name: staff; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.staff ("Id", "Name", "Username", "PasswordHash", "Role") FROM stdin;
c0a0d489-a810-408c-95de-5b3380e49b83	แคชเชียร์	cashier	AQAAAAIAAYagAAAAEIMgewTO8GhojZlyHRnYwWv2s787SoB3/Tmt7hQ9YedJUYl35u20t0QtKOLMeJ1yRQ==	Cashier
ffc495b4-225f-41a2-b24c-82844625c0d6	ผู้จัดการร้าน	manager	AQAAAAIAAYagAAAAEP4sQBtzcCNpNNUUFjfZinrJmnzTO7RA7lHQCoevSjDpL7X5WZW13yze9ETCdcDHvQ==	Manager
\.


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: conditional_promotion_lines PK_conditional_promotion_lines; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.conditional_promotion_lines
    ADD CONSTRAINT "PK_conditional_promotion_lines" PRIMARY KEY ("Id");


--
-- Name: conditional_promotions PK_conditional_promotions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.conditional_promotions
    ADD CONSTRAINT "PK_conditional_promotions" PRIMARY KEY ("Id");


--
-- Name: members PK_members; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.members
    ADD CONSTRAINT "PK_members" PRIMARY KEY ("Id");


--
-- Name: products PK_products; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.products
    ADD CONSTRAINT "PK_products" PRIMARY KEY ("Id");


--
-- Name: promotions PK_promotions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.promotions
    ADD CONSTRAINT "PK_promotions" PRIMARY KEY ("Id");


--
-- Name: sale_applied_promotions PK_sale_applied_promotions; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sale_applied_promotions
    ADD CONSTRAINT "PK_sale_applied_promotions" PRIMARY KEY ("Id");


--
-- Name: sale_line_items PK_sale_line_items; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sale_line_items
    ADD CONSTRAINT "PK_sale_line_items" PRIMARY KEY ("Id");


--
-- Name: sales PK_sales; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sales
    ADD CONSTRAINT "PK_sales" PRIMARY KEY ("Id");


--
-- Name: staff PK_staff; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.staff
    ADD CONSTRAINT "PK_staff" PRIMARY KEY ("Id");


--
-- Name: IX_conditional_promotion_lines_ConditionalPromotionId_ProductId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_conditional_promotion_lines_ConditionalPromotionId_ProductId" ON public.conditional_promotion_lines USING btree ("ConditionalPromotionId", "ProductId");


--
-- Name: IX_members_PhoneNumber; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_members_PhoneNumber" ON public.members USING btree ("PhoneNumber");


--
-- Name: IX_products_Barcode; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_products_Barcode" ON public.products USING btree ("Barcode") WHERE ("Barcode" IS NOT NULL);


--
-- Name: IX_sale_applied_promotions_SaleId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_sale_applied_promotions_SaleId" ON public.sale_applied_promotions USING btree ("SaleId");


--
-- Name: IX_sale_line_items_SaleId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_sale_line_items_SaleId" ON public.sale_line_items USING btree ("SaleId");


--
-- Name: IX_staff_Username; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_staff_Username" ON public.staff USING btree ("Username");


--
-- Name: conditional_promotion_lines FK_conditional_promotion_lines_conditional_promotions_Conditio~; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.conditional_promotion_lines
    ADD CONSTRAINT "FK_conditional_promotion_lines_conditional_promotions_Conditio~" FOREIGN KEY ("ConditionalPromotionId") REFERENCES public.conditional_promotions("Id") ON DELETE CASCADE;


--
-- Name: sale_applied_promotions FK_sale_applied_promotions_sales_SaleId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sale_applied_promotions
    ADD CONSTRAINT "FK_sale_applied_promotions_sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES public.sales("Id") ON DELETE CASCADE;


--
-- Name: sale_line_items FK_sale_line_items_sales_SaleId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sale_line_items
    ADD CONSTRAINT "FK_sale_line_items_sales_SaleId" FOREIGN KEY ("SaleId") REFERENCES public.sales("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--


