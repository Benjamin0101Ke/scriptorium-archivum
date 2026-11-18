import type { Avatar, Tema, User } from '../api';
import { error, LocalAccount } from './datos';
import { demoSession, setDemoSession } from './almacenamiento';

const builtinUsers: User[] = [
  {
    id: 'biblioteca@demo.local',
    correo: 'biblioteca@demo.local',
    nombre: 'Biblioteca · demostración',
    rol: 'Biblioteca',
  },
  {
    id: 'lector@demo.local',
    correo: 'lector@demo.local',
    nombre: 'Lector · demostración',
    rol: 'Lector',
  },
];
async function verifier(password: string, salt: string): Promise<string> {
  const encode = (text: string) => new TextEncoder().encode(text);
  const key = await crypto.subtle.importKey('raw', encode(password), 'PBKDF2', false, [
    'deriveBits',
  ]);
  const bits = await crypto.subtle.deriveBits(
    { name: 'PBKDF2', salt: encode(salt), iterations: 600_000, hash: 'SHA-256' },
    key,
    256,
  );
  return Array.from(new Uint8Array(bits), (byte) => byte.toString(16).padStart(2, '0')).join('');
}

export function currentUser(accounts: LocalAccount[]): User | undefined {
  const id = demoSession();
  return (
    accounts.find((account) => account.user.id === id)?.user ??
    builtinUsers.find((user) => user.id === id)
  );
}

export function requireUser(user: User | undefined): User {
  return user ?? error('Iniciá sesión de demostración para continuar.', {}, 401);
}

export async function handleAccount(
  path: string,
  accounts: LocalAccount[],
  body: { nombres?: string; apellidos?: string; nombre?: string; correo: string; password: string; avatar?: Avatar; tema?: Tema; actual?: string; nueva?: string },
  save: () => void,
) {
  if (path === '/auth/me') return requireUser(currentUser(accounts));
  if (path === '/auth/login') {
    const correo = body.correo.trim().toLowerCase();
    const builtin = builtinUsers.find(
      (person) => person.correo === correo && body.password === 'demo1234',
    );
    const local = accounts.find((account) => account.user.correo === correo);
    const authenticated = local
      ? (await verifier(body.password, local.salt)) === local.verifier ? local.user : null
      : builtin;
    if (!authenticated) error('El correo o la contraseña no coinciden.');
    setDemoSession(authenticated.id);
    return authenticated;
  }
  if (path === '/auth/registro') {
    const nombre = body.nombre?.trim() || [body.nombres?.trim(), body.apellidos?.trim()].filter(Boolean).join(' '),
      correo = body.correo.trim().toLowerCase();
    if (!nombre || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(correo) || body.password.length < 8)
      error('Ingresá tu nombre, un correo válido y una contraseña de al menos 8 caracteres.');
    if (
      builtinUsers.some((person) => person.correo === correo) ||
      accounts.some((item) => item.user.correo === correo)
    )
      error('Ya existe una cuenta local con ese correo.');
    const account: User = { id: crypto.randomUUID(), nombre, correo, rol: 'Lector' };
    const salt = crypto.randomUUID();
    accounts.push({ user: account, salt, verifier: await verifier(body.password, salt) });
    save();
    setDemoSession(account.id);
    return account;
  }
  if (path === '/auth/perfil' || path === '/auth/contrasena') {
    const user = requireUser(currentUser(accounts));
    const local = accounts.find(item => item.user.id === user.id);
    const salt = crypto.randomUUID();
    if (path === '/auth/perfil') {
      if (body.nombre !== undefined && (!body.nombre.trim() || body.nombre.trim().length > 120))
        error('Ingresá un nombre visible de hasta 120 caracteres.');
      if (body.avatar && !['turquesa', 'violeta'].includes(body.avatar) ||
          body.tema && !['sistema', 'claro', 'oscuro'].includes(body.tema))
        error('Elegí un avatar y un tema disponibles.');
      const updated = {
        ...user, nombre: body.nombre?.trim() ?? user.nombre,
        avatar: body.avatar ?? user.avatar, tema: body.tema ?? user.tema,
      };
      if (local) local.user = updated;
      else accounts.push({ user: updated, salt, verifier: await verifier('demo1234', salt) });
      save();
      return updated;
    }
    const actual = body.actual ?? '';
    const verified = local ? await verifier(actual, local.salt) === local.verifier : actual === 'demo1234';
    if (!verified) error('La contraseña actual es incorrecta.');
    if (!body.nueva || body.nueva.length < 8 || body.nueva.length > 512 || body.nueva === actual)
      error('Elegí una contraseña distinta de entre 8 y 512 caracteres.');
    const replacement = { user, salt, verifier: await verifier(body.nueva, salt) };
    if (local) Object.assign(local, replacement);
    else accounts.push(replacement);
    save();
    return { ok: true };
  }
  if (path === '/auth/logout') {
    setDemoSession(null);
    return { ok: true };
  }

  error('Operación de cuenta no disponible.');
}
