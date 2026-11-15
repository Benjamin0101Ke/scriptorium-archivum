import test from 'node:test';
import assert from 'node:assert/strict';
import {validateMetadata} from '../src/app/core/metadata.ts';
const valid={titulo:'Gestión académica',autores:['María Ñúñez','Óscar Pérez'],anio:2026,carrera:'Sistemas',area:'Datos',tipo:'Trabajo de grado',resumen:'Resumen con tildes y eñes.'};
test('acepta los siete metadatos y conserva tildes, eñes y varios autores',()=>{const original=structuredClone(valid);assert.deepEqual(validateMetadata(valid),[]);assert.deepEqual(valid,original);});
for(const field of ['titulo','carrera','area','tipo','resumen'])test('rechaza '+field+' vacío o con espacios',()=>assert.deepEqual(validateMetadata({...valid,[field]:'   '}),[field]));
test('exige al menos un autor',()=>assert.deepEqual(validateMetadata({...valid,autores:[]}),['autores']));
test('rechaza un autor compuesto solo por espacios',()=>assert.deepEqual(validateMetadata({...valid,autores:['María',' ']}),['autores']));
test('rechaza años que no tienen cuatro dígitos enteros',()=>{for(const anio of [26,10000,2026.5,NaN])assert.deepEqual(validateMetadata({...valid,anio}),['anio']);});
