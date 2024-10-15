create table users
(
    id         BLOB not null
        constraint users_pk
            primary key,
    name       TEXT not null,
    public_key BLOB not null
);

create table keys
(
    user_id       BLOB not null
        constraint keys_users_id_fk_user
            references users
            on update cascade on delete cascade,
    target_id     BLOB not null
        constraint keys_users_id_fk_target
            references users
            on update cascade on delete cascade,
    encrypted_key BLOB not null,
    version       INT  not null,
    constraint keys_pk
        primary key (user_id, target_id, version)
);

create table messages
(
    sender_id         BLOB not null
        constraint messages_users_id_fk_sender
            references users
            on update cascade on delete cascade,
    receiver_id       BLOB not null
        constraint messages_users_id_fk_receiver
            references users
            on update cascade on delete cascade,
    message_id        INT  not null,
    encrypted_message BLOB not null,
    timestamp         TEXT not null,
    key_version       INT  not null,
    deleted           INT  not null,
    constraint messages_pk
        primary key (sender_id, receiver_id, message_id)
);


