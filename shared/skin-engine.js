(() => {
  'use strict';
  if (globalThis.ClaudeSkin) return;
  let media = null, layer = null, style = null, settings = null;
  const reduced = matchMedia('(prefers-reduced-motion: reduce)');
  function remove() {
    media?.pause?.();
    layer?.remove(); style?.remove();
    media = layer = style = settings = null;
    document.documentElement.removeAttribute('data-claudeskin');
    document.removeEventListener('visibilitychange', visibility);
    reduced.removeEventListener('change', rebuild);
  }
  function visibility() {
    if (media?.tagName !== 'VIDEO') return;
    if (document.hidden || reduced.matches || !settings.motion) media.pause();
    else media.play().catch(() => {});
  }
  function safeMedia(value) {
    if (typeof value !== 'string') return false;
    if (/^data:(image\/(png|jpeg|webp|gif)|video\/(mp4|webm));base64,[a-z0-9+/=]+$/i.test(value)) return true;
    try {
      const url = new URL(value, location.href);
      return url.origin === location.origin && ['http:', 'https:', 'file:'].includes(url.protocol);
    } catch { return false; }
  }
  function rebuild() { if (settings) apply(settings); }
  function apply(input) {
    remove();
    if (!input?.enabled) return;
    if (!safeMedia(input.poster) || !safeMedia(input.media) || typeof input.css !== 'string') throw new Error('Invalid theme payload');
    settings = {...input, dim: Math.min(.85, Math.max(.1, Number(input.dim) || .45))};
    style = document.createElement('style');
    style.id = 'claudeskin-style'; style.textContent = settings.css;
    (document.head || document.documentElement).append(style);
    layer = document.createElement('div');
    layer.id = 'claudeskin-background'; layer.setAttribute('aria-hidden', 'true');
    const animate = settings.motion && !reduced.matches;
    const video = animate && settings.kind === 'video';
    media = document.createElement(video ? 'video' : 'img');
    if (video) { media.muted = true; media.loop = true; media.autoplay = true; media.playsInline = true; media.poster = settings.poster; }
    else media.alt = '';
    media.src = animate ? settings.media : settings.poster;
    const currentMedia = media;
    media.addEventListener('error', () => {
      if (media !== currentMedia || !settings) return;
      const fallback = document.createElement('img');
      fallback.alt = ''; fallback.src = settings?.poster || input.poster;
      media?.pause?.(); media?.replaceWith(fallback); media = fallback;
    }, {once: true});
    layer.append(media);
    const veil = document.createElement('div'); veil.className = 'claudeskin-veil'; veil.style.opacity = String(settings.dim);
    layer.append(veil);
    document.documentElement.append(layer);
    document.documentElement.setAttribute('data-claudeskin', '');
    document.addEventListener('visibilitychange', visibility);
    reduced.addEventListener('change', rebuild);
    visibility();
  }
  globalThis.ClaudeSkin = Object.freeze({apply, remove});
})();
