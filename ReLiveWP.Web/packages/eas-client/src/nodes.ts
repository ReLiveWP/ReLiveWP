export {
    child,
    childrenNamed,
    el,
    isNode,
    isOpaque,
    isText,
    opaque,
    opaqueOf,
    textOf,
    type EasChild,
    type EasNode,
    type EasOpaque,
    type EasText,
} from './nodes/node.ts';
export { makeTags, opt, type Kid, type Tag, type TagSet } from './nodes/tags.ts';
export {
    bytes,
    elements,
    flag,
    int,
    path,
    pick,
    pickAll,
    text,
    tri,
} from './nodes/read.ts';
export { readDate, writeDate, type DateFormat } from './nodes/dates.ts';
export { mergeProperties, type PropertySpec } from './nodes/merge.ts';
export { decode, type DecodeResult, type UnknownToken } from './wbxml/decode.ts';
export { encode } from './wbxml/encode.ts';
export { WbxmlError } from './wbxml/errors.ts';
export { toXml, type ToXmlOptions } from './wbxml/toxml.ts';
export * as tags from './generated/tags.g.ts';
export { TAG_VERSIONS } from './generated/tag-versions.g.ts';
