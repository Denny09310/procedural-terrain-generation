let canvas, ctx, dotnet, chunkSize, tileSize;

let zoom = 1.0;
let panX = 0;
let panY = 0;

let isDragging = false;
let startX = 0;
let startY = 0;

const cache = new Map();
const pending = new Set();
const visible = new Set();

const radius = 2;

let previousBounds = null;

export function init(element, instance, options) {
    canvas = element;

    ctx = canvas.getContext("2d", {
        alpha: false,
        desynchronized: true
    });

    dotnet = instance;
    chunkSize = options.chunkSize;
    tileSize = options.tileSize;

    resizeCanvas();

    window.addEventListener("resize", resizeCanvas);

    canvas.addEventListener("pointerdown", onPointerDown);
    canvas.addEventListener("pointermove", onPointerMove);
    canvas.addEventListener("pointerup", onPointerUp);
    canvas.addEventListener("pointercancel", onPointerUp);
    canvas.addEventListener("wheel", onWheel, { passive: false });

    requestAnimationFrame(renderLoop);
}

function resizeCanvas() {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
}

// -----------------------------------------------------------------------------
// Interaction
// -----------------------------------------------------------------------------

function onPointerDown(e) {
    isDragging = true;
    canvas.setPointerCapture(e.pointerId);

    startX = e.clientX - panX;
    startY = e.clientY - panY;
}

function onPointerMove(e) {
    if (!isDragging)
        return;

    panX = e.clientX - startX;
    panY = e.clientY - startY;
}

function onPointerUp(e) {
    if (!isDragging)
        return;

    isDragging = false;
    canvas.releasePointerCapture(e.pointerId);
}

function onWheel(e) {
    e.preventDefault();

    const zoomFactor = 1.15;

    const mouseX = e.clientX;
    const mouseY = e.clientY;

    const worldX = (mouseX - panX) / zoom;
    const worldY = (mouseY - panY) / zoom;

    if (e.deltaY < 0) {
        zoom *= zoomFactor;
    } else {
        zoom /= zoomFactor;
    }

    zoom = Math.max(0.05, Math.min(zoom, 15.0));

    panX = mouseX - worldX * zoom;
    panY = mouseY - worldY * zoom;
}

// -----------------------------------------------------------------------------
// Rendering
// -----------------------------------------------------------------------------

function parse(key) {
    return key.split(",").map(Number);
}

function updateVisibleChunks(minChunkX, maxChunkX, minChunkY, maxChunkY) {

    const next = new Set();

    for (let y = minChunkY; y <= maxChunkY; y++) {

        for (let x = minChunkX; x <= maxChunkX; x++) {

            next.add(`${x},${y}`);
        }
    }

    //
    // Load newly visible chunks
    //

    const requests = [];

    const centerWorldX =
        (-panX / zoom) + canvas.width / (2 * zoom);

    const centerWorldY =
        (-panY / zoom) + canvas.height / (2 * zoom);

    const centerChunkX =
        Math.floor(centerWorldX / chunkSize);

    const centerChunkY =
        Math.floor(centerWorldY / chunkSize);

    for (const key of next) {

        if (visible.has(key))
            continue;

        visible.add(key);

        if (cache.has(key) || pending.has(key))
            continue;

        const [x, y] = parse(key);

        requests.push({
            key,
            x,
            y,
            priority:
                (x - centerChunkX) ** 2 +
                (y - centerChunkY) ** 2
        });
    }

    requests.sort((a, b) => a.priority - b.priority);

    for (const request of requests) {

        pending.add(request.key);

        startRendering(
            request.x,
            request.y);
    }

    //
    // Unload chunks leaving the visible area
    //

    const removed = [];

    for (const key of visible) {

        if (next.has(key))
            continue;

        visible.delete(key);

        const [x, y] = parse(key);
        removed.push({ x, y });

        const bitmap = cache.get(key);

        if (bitmap) {
            bitmap.close();
            cache.delete(key);
        }

        pending.delete(key);
    }

    if (removed.length > 0) {

        dotnet.invokeMethodAsync(
            "UnloadChunks",
            removed)
            .catch(console.error);
    }
}

function renderLoop() {

    if (!canvas)
        return;

    ctx.imageSmoothingEnabled = false;

    ctx.clearRect(0, 0, canvas.width, canvas.height);

    ctx.save();

    ctx.translate(panX, panY);
    ctx.scale(zoom, zoom);

    const worldChunkSize = chunkSize * tileSize;

    const minWorldX = -panX / zoom;
    const maxWorldX = (canvas.width - panX) / zoom;

    const minWorldY = -panY / zoom;
    const maxWorldY = (canvas.height - panY) / zoom;

    const minChunkX =
        Math.floor(minWorldX / worldChunkSize) - radius;

    const maxChunkX =
        Math.floor(maxWorldX / worldChunkSize) + radius;

    const minChunkY =
        Math.floor(minWorldY / worldChunkSize) - radius;

    const maxChunkY =
        Math.floor(maxWorldY / worldChunkSize) + radius;

    updateVisibleChunks(
        minChunkX,
        maxChunkX,
        minChunkY,
        maxChunkY);

    for (const key of visible) {

        const bitmap = cache.get(key);

        if (!bitmap)
            continue;

        const [cx, cy] = parse(key);

        ctx.drawImage(
            bitmap,
            cx * worldChunkSize,
            cy * worldChunkSize,
            worldChunkSize,
            worldChunkSize);
    }

    ctx.restore();

    requestAnimationFrame(renderLoop);
}

// -----------------------------------------------------------------------------
// Synchronization
// -----------------------------------------------------------------------------

function startRendering(cx, cy) {

    dotnet.invokeMethodAsync("StartRendering", cx, cy)
        .catch(err => {

            pending.delete(`${cx},${cy}`);

            console.error(err);
        });
}

export function endRendering(cx, cy, biomes, structures) {

    const key = `${cx},${cy}`;

    pending.delete(key);

    createChunkBitmap(biomes, structures)
        .then(bitmap => {

            const previous = cache.get(key);

            if (previous) {
                previous.close();
            }

            cache.set(key, bitmap);
        })
        .catch(err => console.error(err));
}

// -----------------------------------------------------------------------------
// Bitmap generation
// -----------------------------------------------------------------------------

async function createChunkBitmap(biomes, structures) {

    const totalCells = chunkSize * chunkSize;

    const pixels = new Uint8ClampedArray(totalCells * 4);

    for (let i = 0; i < totalCells; i++) {

        let color = getBiomeColor(biomes[i]);

        if (structures[i] > 0) {
            color = getStructureColor(structures[i]);
        }

        const offset = i * 4;

        pixels[offset] = color.r;
        pixels[offset + 1] = color.g;
        pixels[offset + 2] = color.b;
        pixels[offset + 3] = 255;
    }

    return createImageBitmap(
        new ImageData(
            pixels,
            chunkSize,
            chunkSize));
}

// -----------------------------------------------------------------------------
// Cache
// -----------------------------------------------------------------------------

export function clearCache() {

    for (const bitmap of cache.values()) {
        bitmap.close();
    }

    cache.clear();
    pending.clear();
    visible.clear();
}

export function dispose() {

    window.removeEventListener("resize", resizeCanvas);

    clearCache();

    canvas = null;
    ctx = null;
}

// -----------------------------------------------------------------------------
// Colors
// -----------------------------------------------------------------------------

function getBiomeColor(biome) {

    switch (biome) {
        case 0: return { r: 0, g: 105, b: 148 };
        case 1: return { r: 30, g: 144, b: 255 };
        case 2: return { r: 238, g: 214, b: 175 };
        case 3: return { r: 210, g: 180, b: 140 };
        case 4: return { r: 50, g: 205, b: 50 };
        case 5: return { r: 34, g: 139, b: 34 };
        case 6: return { r: 120, g: 120, b: 120 };
        case 7: return { r: 90, g: 90, b: 90 };
        case 8: return { r: 255, g: 255, b: 255 };
        default: return { r: 255, g: 0, b: 255 };
    }
}

function getStructureColor(structure) {

    switch (structure) {
        case 1: return { r: 220, g: 20, b: 60 };
        case 2: return { r: 139, g: 69, b: 19 };
        case 3: return { r: 105, g: 105, b: 105 };
        default: return { r: 255, g: 255, b: 0 };
    }
}