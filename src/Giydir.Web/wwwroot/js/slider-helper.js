window.initBeforeAfterSlider = (container) => {
    if (!container) return;

    const handle = container.querySelector('.slider-handle');
    const foreground = container.querySelector('.slider-foreground');
    const background = container.querySelector('.slider-background'); // This might just be for context, but we move foreground width.

    if (!handle || !foreground) return;

    let isDragging = false;

    const start = (e) => {
        isDragging = true;
        update(e);
        e.preventDefault(); // Prevent text selection etc.
    };

    const stop = () => {
        isDragging = false;
    };

    const update = (e) => {
        if (!isDragging) return;

        const rect = container.getBoundingClientRect();
        // Client X for either mouse or touch
        let clientX = e.clientX;
        if (e.touches && e.touches.length > 0) {
            clientX = e.touches[0].clientX;
        }

        let offsetX = clientX - rect.left;

        // Clamp
        if (offsetX < 0) offsetX = 0;
        if (offsetX > rect.width) offsetX = rect.width;

        const percentage = (offsetX / rect.width) * 100;

        foreground.style.width = percentage + '%';
        handle.style.left = percentage + '%';
    };

    // Attach events directly to window for global drag
    window.addEventListener('mouseup', stop);
    window.addEventListener('touchend', stop);

    // Attach move events to window too for smoothness even if mouse leaves div area
    window.addEventListener('mousemove', update);
    window.addEventListener('touchmove', update);

    // Initial attach to elements
    container.addEventListener('mousedown', start);
    container.addEventListener('touchstart', start);

    // Cleanup function? 
    // Blazor components might dispose, so we might want to return a dispose handle.
    // For now simple global listener attach might cause leak if component re-renders many times.
    // Let's attach move/up temporarily on down.

    // Better Approach for encapsulation:
    // Remove global listeners first.
    window.removeEventListener('mouseup', stop);
    window.removeEventListener('touchend', stop);
    window.removeEventListener('mousemove', update);
    window.removeEventListener('touchmove', update);

    const onMove = (e) => {
        if (!isDragging) return;
        update(e);
    };

    const onUp = () => {
        isDragging = false;
        window.removeEventListener('mousemove', onMove);
        window.removeEventListener('touchmove', onMove);
        window.removeEventListener('mouseup', onUp);
        window.removeEventListener('touchend', onUp);
    };

    const onDown = (e) => {
        isDragging = true;
        update(e);
        window.addEventListener('mousemove', onMove);
        window.addEventListener('touchmove', onMove);
        window.addEventListener('mouseup', onUp);
        window.addEventListener('touchend', onUp);
        e.preventDefault();
    };

    container.removeEventListener('mousedown', onDown); // clear old possibly? No reference.
    container.addEventListener('mousedown', onDown);
    container.addEventListener('touchstart', onDown);
};
