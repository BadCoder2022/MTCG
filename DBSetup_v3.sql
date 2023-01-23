drop table if exists score;
drop table if exists stack;
drop table if exists gamer;
drop table if exists cpackage;
drop table if exists card;

drop type if exists element_type cascade;
drop type if exists monster_type cascade;
drop type if exists card_type cascade;


create extension if not exists "pgcrypto";
create extension if not exists "uuid-ossp";

create table if not exists card(
	--id integer primary key generated always as identity,
	id uuid primary key DEFAULT uuid_generate_v4 (),
	type integer NOT NULL,
	name text NOT NULL,
	damage real NOT NULL,
	isMonster bool NOT NULL
);

create table if not exists gamer(
	id integer generated always as identity,
	name text unique primary key not null,
	coins integer,
	token text ,
	password text not null,
	alias text,
	bio text,
	image text
);

create table if not exists stack(
	id integer primary key generated always as identity,
	gamer name references gamer,
	card uuid references card,
	partOfDeck boolean
); 

create table if not exists cpackage(
	id integer primary key generated always as identity,
	c0id uuid references card,
	c1id uuid references card,
	c2id uuid references card,
	c3id uuid references card,
	c4id uuid references card
);  

create table if not exists score(
	id integer primary key generated always as identity,
	gamer name REFERENCES gamer,
	wins INTEGER,
	losses integer,
	draws INTEGER,
	Elo	integer
);

create table if not exists trades(
	id uuid primary key not null,
	card uuid references card,
	wantsMonster bool not null,
	minDamage real not NULL
	);
	
	create table if not exists Randomcard(
	id uuid primary key DEFAULT uuid_generate_v4 (),
	type integer NOT NULL,
	name text NOT NULL,
	damage real NOT NULL,
	isMonster bool NOT NULL
);
