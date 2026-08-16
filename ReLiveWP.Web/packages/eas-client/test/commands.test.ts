import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { buildFolderSync, parseFolderSync, type FolderChange } from '../src/commands/foldersync.ts';
import {
    buildItemOperations,
    parseItemOperations,
    DOCUMENT_LIBRARY,
    MAILBOX,
    type EmptyFolderContentsResponse,
    type FetchResponse,
    type MoveResponse,
} from '../src/commands/itemoperations.ts';
import { buildPing, parsePing } from '../src/commands/ping.ts';
import { buildSync, parseSync } from '../src/commands/sync.ts';
import { elements } from '../src/nodes/read.ts';
import {
    AirSync as A,
    AirSyncBase as AB,
    FolderHierarchy as F,
    ItemOperations as IO,
    Ping,
} from '../src/generated/tags.g.ts';
import { decode } from '../src/wbxml/decode.ts';
import { encode } from '../src/wbxml/encode.ts';
import { child, opaque, textOf, type EasNode } from '../src/nodes/node.ts';

const names = (node: EasNode | undefined) => elements(node).map((e) => e.name);
const collectionOf = (root: EasNode) => child(child(root, 'AirSync', 'Collections')!, 'AirSync', 'Collection')!;

describe('buildSync', () => {
    it('emits Collection children in the order the schema declares', () => {
        const collection = collectionOf(buildSync({
            collections: [{
                collectionId: 'inbox',
                syncKey: '3',
                windowSize: 50,
                getChanges: true,
                deletesAsMoves: true,
                conversationMode: false,
                options: [{ filterType: 1 }],
                commands: [{ kind: 'Delete', serverId: 'a' }],
            }],
        }));

        assert.deepEqual(names(collection), [
            'SyncKey', 'CollectionId', 'DeletesAsMoves', 'GetChanges',
            'WindowSize', 'ConversationMode', 'Options', 'Commands',
        ]);
    });

    it('omits everything not asked for', () => {
        const collection = collectionOf(buildSync({ collections: [{ collectionId: 'inbox', syncKey: '0' }] }));
        assert.deepEqual(names(collection), ['SyncKey', 'CollectionId']);
    });

    it('scopes Class inside Options, not directly under Collection', () => {
        const collection = collectionOf(buildSync({
            collections: [{ collectionId: 'contacts', syncKey: '1', options: [{ class: 'Contacts', filterType: 1 }] }],
        }));

        assert.equal(child(collection, 'AirSync', 'Class'), undefined);
        assert.deepEqual(names(child(collection, 'AirSync', 'Options')), ['Class', 'FilterType']);
    });

    it('carries two Options, which the schema allows so each can name a class', () => {
        const collection = collectionOf(buildSync({
            collections: [{
                collectionId: 'inbox',
                syncKey: '1',
                options: [{ class: 'Email', filterType: 2 }, { class: 'SMS', filterType: 0 }],
            }],
        }));

        assert.deepEqual(names(collection).filter((n) => n === 'Options'), ['Options', 'Options']);
    });

    it('nests a cross-namespace BodyPreference', () => {
        const collection = collectionOf(buildSync({
            collections: [{
                collectionId: 'inbox',
                syncKey: '1',
                options: [{ bodyPreference: [{ type: 2, truncationSize: 512 }] }],
            }],
        }));

        const preference = child(child(collection, 'AirSync', 'Options')!, 'AirSyncBase', 'BodyPreference')!;
        assert.equal(preference.ns, 'AirSyncBase');
        assert.deepEqual(names(preference), ['Type', 'TruncationSize']);
    });

    it('subscribes to live annotations with Name and no Value, last in Options', () => {
        const options = child(collectionOf(buildSync({
            collections: [{
                collectionId: 'contacts',
                syncKey: '0',
                options: [{
                    bodyPreference: [{ type: 1 }],
                    annotations: ['Type', 'FavoriteOrder'],
                }],
            }],
        })), 'AirSync', 'Options')!;

        assert.deepEqual(names(options), ['BodyPreference', 'Annotations']);

        const annotations = child(options, 'WindowsLive', 'Annotations')!;
        assert.deepEqual(names(annotations), ['Annotation', 'Annotation']);

        const [first, second] = elements(annotations);
        // Name alone is what makes it a subscription rather than an assignment
        assert.deepEqual(names(first), ['Name']);
        assert.equal(textOf(child(first!, 'WindowsLive', 'Name')!), 'Type');
        assert.equal(textOf(child(second!, 'WindowsLive', 'Name')!), 'FavoriteOrder');
    });

    it('omits the annotations block when there is nothing to subscribe to', () => {
        for (const annotations of [undefined, []]) {
            const options = child(collectionOf(buildSync({
                collections: [{ collectionId: 'contacts', syncKey: '0', options: [{ annotations, filterType: 0 }] }],
            })), 'AirSync', 'Options')!;

            assert.deepEqual(names(options), ['FilterType']);
        }
    });

    it('round-trips the annotation subscription through code page 254', () => {
        const built = buildSync({
            collections: [{
                collectionId: 'contacts',
                syncKey: '0',
                options: [{ annotations: ['Type', 'CID', 'UserTileUrl'] }],
            }],
        });

        assert.deepEqual(decode(encode(built)).root, built);
    });

    it('builds each command kind', () => {
        const commands = child(collectionOf(buildSync({
            collections: [{
                collectionId: 'inbox',
                syncKey: '1',
                commands: [
                    { kind: 'Add', clientId: 'c1', applicationData: A.ApplicationData(AB.Body('hello')) },
                    { kind: 'Change', serverId: 's1', applicationData: A.ApplicationData(AB.Body('bye')) },
                    { kind: 'Delete', serverId: 's2' },
                    { kind: 'SoftDelete', serverId: 's3' },
                    { kind: 'Fetch', serverId: 's4' },
                ],
            }],
        })), 'AirSync', 'Commands');

        assert.deepEqual(names(commands), ['Add', 'Change', 'Delete', 'SoftDelete', 'Fetch']);
    });

    it('round-trips through the codec', () => {
        const built = buildSync({
            collections: [{ collectionId: 'inbox', syncKey: '3', windowSize: 50, getChanges: true }],
            wait: 10,
        });
        assert.deepEqual(decode(encode(built)).root, built);
    });
});

describe('parseSync', () => {
    const response = A.Sync(A.Collections(A.Collection(
        A.SyncKey('4'),
        A.CollectionId('inbox'),
        A.Status(1),
        A.MoreAvailable(),
        A.Responses(A.Add(A.ClientId('c1'), A.ServerId('s9'), A.Status(1))),
        A.Commands(
            A.Add(A.ServerId('s1'), A.ApplicationData(AB.Body('one'))),
            A.Change(A.ServerId('s2'), A.ApplicationData(AB.Body('two'))),
            A.Delete(A.ServerId('s3')),
            A.SoftDelete(A.ServerId('s4'))))));

    it('reads the collection envelope', () => {
        const parsed = parseSync(response);
        const collection = parsed.collections[0]!;

        assert.equal(parsed.collections.length, 1);
        assert.equal(collection.syncKey, '4');
        assert.equal(collection.collectionId, 'inbox');
        assert.equal(collection.status, 1);
        assert.equal(collection.moreAvailable, true);
    });

    it('keeps changes in wire order and holds ApplicationData as a tree', () => {
        const collection = parseSync(response).collections[0]!;

        assert.deepEqual(collection.changes.map((c) => [c.kind, c.serverId]), [
            ['Add', 's1'], ['Change', 's2'], ['Delete', 's3'], ['SoftDelete', 's4'],
        ]);

        const add = collection.changes[0]!;
        assert.equal(add.kind, 'Add');
        assert.equal(add.kind === 'Add' ? add.applicationData?.name : undefined, 'ApplicationData');
    });

    it('reads Responses separately from Commands', () => {
        const collection = parseSync(response).collections[0]!;
        assert.deepEqual(collection.responses, [
            { kind: 'Add', clientId: 'c1', serverId: 's9', status: 1, applicationData: undefined },
        ]);
    });

    it('survives a response with nothing in it', () => {
        const parsed = parseSync(A.Sync());
        assert.deepEqual(parsed.collections, []);
        assert.equal(parsed.status, undefined);
    });
});

describe('FolderSync', () => {
    it('builds a request', () => {
        assert.deepEqual(buildFolderSync('0'), F.FolderSync(F.SyncKey('0')));
    });

    it('parses changes and picks out the adds', () => {
        const parsed = parseFolderSync(F.FolderSync(
            F.Status(1),
            F.SyncKey('1'),
            F.Changes(
                F.Count(2),
                F.Add(F.ServerId('2'), F.ParentId('0'), F.DisplayName('Inbox'), F.Type(2)),
                F.Add(F.ServerId('3'), F.ParentId('0'), F.DisplayName('Drafts'), F.Type(3)),
                F.Delete(F.ServerId('9')))));

        assert.equal(parsed.status, 1);
        assert.equal(parsed.syncKey, '1');
        assert.equal(parsed.count, 2);
        assert.deepEqual(parsed.changes.map((c) => c.kind), ['Add', 'Add', 'Delete']);
        assert.deepEqual(parsed.changes.filter((c: FolderChange) => c.kind === 'Add').map((f) => f.displayName), ['Inbox', 'Drafts']);
        assert.equal(parsed.changes.filter((c: FolderChange) => c.kind === 'Add')[0]!.type, 2);
    });

    it('survives an empty response', () => {
        const parsed = parseFolderSync(F.FolderSync());
        assert.deepEqual(parsed.changes, []);
        assert.equal(parsed.count, undefined);
    });
});

describe('Ping', () => {
    it('builds the heartbeat and the monitored folder list', () => {
        const root = buildPing({
            heartbeatInterval: 150,
            folders: [{ id: '2', class: 'Email' }, { id: '8', class: 'Calendar' }],
        });

        assert.deepEqual(names(root), ['HeartbeatInterval', 'Folders']);
        const folders = child(root, 'Ping', 'Folders')!;
        assert.deepEqual(names(folders), ['Folder', 'Folder']);
        assert.deepEqual(names(elements(folders)[0]), ['Id', 'Class']);
    });

    it('omits both halves when the cached values are wanted', () => {
        assert.deepEqual(names(buildPing({})), []);
    });

    it('reads the changed folder ids out of a response, where Folder is text', () => {
        const parsed = parsePing(Ping.Ping(
            Ping.Status(2),
            Ping.Folders(Ping.Folder('2'), Ping.Folder('8'))));

        assert.equal(parsed.status, 2);
        assert.deepEqual(parsed.folderIds, ['2', '8']);
    });

    it('reports the interval and folder cap the server pushes back', () => {
        assert.equal(parsePing(Ping.Ping(Ping.Status(5), Ping.HeartbeatInterval(60))).heartbeatInterval, 60);
        assert.equal(parsePing(Ping.Ping(Ping.Status(6), Ping.MaxFolders(20))).maxFolders, 20);
        assert.deepEqual(parsePing(Ping.Ping(Ping.Status(1))).folderIds, []);
    });
});

describe('ItemOperations', () => {
    it('fetches a mailbox item with the full option set', () => {
        const root = buildItemOperations({
            operations: [{
                kind: 'Fetch',
                store: MAILBOX,
                collectionId: 'inbox',
                serverId: '5',
                options: {
                    mimeSupport: 2,
                    bodyPreference: [{ type: 2, truncationSize: 0 }],
                    bodyPartPreference: [{ type: 2 }],
                    rightsManagementSupport: true,
                },
            }],
        });

        const fetch = elements(root)[0]!;
        assert.deepEqual(names(fetch), ['Store', 'ServerId', 'CollectionId', 'Options']);
        assert.deepEqual(names(child(fetch, 'ItemOperations', 'Options')), [
            'MIMESupport', 'BodyPreference', 'BodyPartPreference', 'RightsManagementSupport',
        ]);
    });

    it('fetches an attachment by file reference with a byte range', () => {
        const root = buildItemOperations({
            operations: [{
                kind: 'Fetch',
                store: MAILBOX,
                fileReference: 'att-1',
                options: { range: { start: 0, end: 1023 } },
            }],
        });

        const fetch = elements(root)[0]!;
        assert.deepEqual(names(fetch), ['Store', 'FileReference', 'Options']);
        assert.equal(
            textOf(child(child(fetch, 'ItemOperations', 'Options')!, 'ItemOperations', 'Range')!),
            '0-1023');
    });

    it('fetches a document library link with credentials', () => {
        const root = buildItemOperations({
            operations: [{
                kind: 'Fetch',
                store: DOCUMENT_LIBRARY,
                linkId: 'https://share/doc.docx',
                options: { userName: 'u', password: 'p', range: { start: 0, end: 99 } },
            }],
        });

        const fetch = elements(root)[0]!;
        assert.deepEqual(names(fetch), ['Store', 'LinkId', 'Options']);
        assert.equal(textOf(child(fetch, 'ItemOperations', 'Store')!), 'Document Library');
        assert.deepEqual(names(child(fetch, 'ItemOperations', 'Options')), ['Range', 'UserName', 'Password']);
    });

    it('fetches a search result by LongId', () => {
        const root = buildItemOperations({
            operations: [{ kind: 'Fetch', store: MAILBOX, longId: 'abc' }],
        });

        assert.deepEqual(names(elements(root)[0]), ['Store', 'LongId']);
    });

    it('empties a folder and moves a conversation in the same batch', () => {
        const root = buildItemOperations({
            operations: [
                { kind: 'EmptyFolderContents', collectionId: 'deleted', deleteSubFolders: true },
                { kind: 'Move', conversationId: 'c1', dstFldId: 'archive', moveAlways: true },
            ],
        });

        assert.deepEqual(names(root), ['EmptyFolderContents', 'Move']);
        assert.deepEqual(names(elements(root)[0]), ['CollectionId', 'Options']);
        assert.deepEqual(names(elements(root)[1]), ['ConversationId', 'DstFldId', 'Options']);
    });

    it('decodes payload bytes whether they arrive opaque or base64', () => {
        const bytes = new Uint8Array([0xde, 0xad, 0xbe, 0xef]);

        const asOpaque = parseItemOperations(IO.ItemOperations(IO.Status(1),
            IO.Response(IO.Fetch(IO.Status(1), IO.Properties(IO.Data(opaque(bytes)))))));
        const asText = parseItemOperations(IO.ItemOperations(IO.Status(1),
            IO.Response(IO.Fetch(IO.Status(1), IO.Properties(IO.Data('3q2+7w=='))))));

        assert.equal(asOpaque.operations[0]!.kind, 'Fetch');
        assert.deepEqual((asOpaque.operations[0] as FetchResponse).data, bytes);
        assert.deepEqual((asText.operations[0] as FetchResponse).data, bytes);
    });

    it('parses all three response kinds and keeps item properties as a tree', () => {
        const parsed = parseItemOperations(IO.ItemOperations(
            IO.Status(1),
            IO.Response(
                IO.Fetch(
                    IO.Status(1),
                    A.Class('Email'),
                    A.CollectionId('inbox'),
                    A.ServerId('5'),
                    IO.Properties(IO.Total(42), AB.Body(AB.Type(2), AB.Data('hello')))),
                IO.EmptyFolderContents(IO.Status(1), A.CollectionId('deleted')),
                IO.Move(IO.Status(3), IO.ConversationId('c1')))));

        assert.deepEqual(parsed.operations.map((o) => o.kind), ['Fetch', 'EmptyFolderContents', 'Move']);

        const fetch = parsed.operations[0] as FetchResponse;
        assert.equal(fetch.class, 'Email');
        assert.equal(fetch.collectionId, 'inbox');
        assert.equal(fetch.serverId, '5');
        assert.equal(fetch.total, 42);
        assert.deepEqual(names(fetch.properties), ['Total', 'Body']);

        assert.equal((parsed.operations[1] as EmptyFolderContentsResponse).collectionId, 'deleted');
        assert.equal((parsed.operations[2] as MoveResponse).conversationId, 'c1');
        assert.equal((parsed.operations[2] as MoveResponse).status, 3);
    });
});
